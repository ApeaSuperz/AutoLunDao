using System;
using System.Collections.Generic;
using System.Linq;
using AutoLunDao.Core.Entities;

namespace AutoLunDao.Core.Strategies;

/// <summary>
///     策略公共工具。
/// </summary>
public static class StrategyUtils
{
    /// <summary>
    ///     返回传入状态的深拷贝，用于安全模拟。
    /// </summary>
    /// <param name="state">要拷贝的状态</param>
    /// <returns>传入状态的深拷贝</returns>
    public static State CreateStateCopy(State state)
    {
        if (state is null) throw new ArgumentNullException(nameof(state));

        var topicsCopy = state.Topics.Select(t => new Topic(t.ID, t.Goals.ToList())).ToList();
        var handCopy = state.Hand.Select(c => new Card(c.TopicID, c.Value)).ToList();
        var tableCopy = state.Table.Select(c => new Card(c.TopicID, c.Value)).ToList();
        return new State(topicsCopy, handCopy, tableCopy, state.TurnsLeft, state.Spaces, state.OriginalTopics);
    }

    /// <summary>
    ///     判断打出某张卡牌，是否会导致错过该论题的最大目标点数。
    /// </summary>
    /// <param name="card">要打出的卡牌</param>
    /// <param name="state">游戏状态</param>
    /// <returns>打出该手牌是否会导致错过其论题的最大目标点数</returns>
    public static bool WillCauseMissOfExistingMaxGoal(Card? card, State? state)
    {
        if (card is null || state is null) return false;

        var topic = state.Topics.FirstOrDefault(t => t.ID == card.TopicID);
        if (topic?.Goals is null || topic.Goals.Count == 0) return false;

        // 只关注该论题的最大目标点数
        var maxGoal = topic.Goals.Max();

        // 取出该论题在桌面上的所有卡
        var table = state.Table.Where(c => c.TopicID == card.TopicID).ToList();

        // 只有当最大目标原本就在桌面上，才考虑「被错过」
        var maxPresentOriginally = table.Any(c => c.Value == maxGoal);
        if (!maxPresentOriginally) return false;

        // 构造打出卡牌后的模拟桌面
        table.Add(card);
        var mergedTable = MergeCardsOnTable(table);

        // 如果合成后最大目标点数不再存在，视为会错过最大目标
        var maxPresentAfter = mergedTable.Any(c => c.TopicID == card.TopicID && c.Value == maxGoal);
        return !maxPresentAfter;
    }

    /// <summary>
    ///     合成桌面上的卡牌。
    ///     按照游戏规则，通常一次只会有两张相同的卡牌合成，但这里使用批量合成，为沙盒等可能需要批量处理的场景做准备。
    /// </summary>
    /// <param name="table">当前桌面</param>
    /// <returns>合成后的桌面</returns>
    public static List<Card> MergeCardsOnTable(List<Card>? table)
    {
        if (table is null) return [];

        // 统计相同卡牌的数量
        var counts = new Dictionary<Card, int>();
        foreach (var key in table)
        {
            counts.TryGetValue(key, out var count);
            counts[key] = count + 1;
        }

        // 批量处理合成对：每次把 pairs 转移到 value+1
        bool changed;
        do
        {
            changed = false;

            // 使用快照键列表，按 value 升序可使合成从低位向高位推进
            var keys = counts.Keys.OrderBy(k => k.Value).ToList();
            foreach (var card in keys)
            {
                if (!counts.TryGetValue(card, out var count) || count < 2) continue;
                var pairs = count / 2;
                counts[card] = count - pairs * 2;
                var upCard = new Card(card.TopicID, card.Value + 1);
                counts.TryGetValue(upCard, out var upCount);
                counts[upCard] = upCount + pairs;
                changed = true;
            }
        } while (changed);

        // 根据最终计数重建合成后的桌面（跳过计数为 0 的项）
        var result = new List<Card>();
        foreach (var kv in counts)
        {
            var card = kv.Key;
            var count = kv.Value;
            for (var i = 0; i < count; i++)
                result.Add(new Card(card.TopicID, card.Value));
        }

        return result;
    }

    /// <summary>
    ///     检查桌面上是否存在指定论题的目标值。
    /// </summary>
    /// <param name="table"></param>
    /// <param name="topicId"></param>
    /// <param name="goalValue"></param>
    /// <returns>指定论题的某个目标值是否已在桌面上</returns>
    public static bool HasGoalOnTable(List<Card> table, int topicId, int goalValue)
    {
        return table.Any(c => c.TopicID == topicId && c.Value == goalValue);
    }

    /// <summary>
    ///     检查论题的所有目标值是否均已完成（所有目标点数都在桌面上）。
    /// </summary>
    /// <param name="table">桌面</param>
    /// <param name="topic">论题</param>
    /// <returns>论题是否已完成</returns>
    public static bool IsTopicGoalsOnTable(List<Card> table, Topic topic)
    {
        return topic.Goals.Count == 0 || topic.Goals.All(g => HasGoalOnTable(table, topic.ID, g));
    }

    /// <summary>
    ///     评估状态变化带来的分数变化。
    /// </summary>
    /// <param name="before">打出手牌前</param>
    /// <param name="after">打出手牌后</param>
    /// <returns>出牌得分</returns>
    public static float EvaluateStateChanges(State before, State after)
    {
        var score = 0f;

        // 1. 空位无变化或变多，说明有合成，给予丰厚奖励，鼓励合成
        if (before.Spaces <= after.Spaces)
            score += (after.Spaces - before.Spaces + 1) * 1000f;

        // 2. 查看论题完成情况，完成论题给大量奖励
        if (before.Topics.Count > after.Topics.Count)
            score += (before.Topics.Count - after.Topics.Count) * 200f;

        // 3. 对于未完成的论题，统计完成进度的提升，给予适当奖励
        // 只对比 after 中的 Topics，避免 before 中已完成的论题影响评分
        var progressDiff = (from topic in after.Topics
            let beforeProgress = _CalculateTopicCompletionProgress(topic, before.Table)
            let afterProgress = _CalculateTopicCompletionProgress(topic, after.Table)
            select afterProgress - beforeProgress).Sum();
        score += (float)progressDiff * 200f;

        return score;
    }

    /// <summary>
    ///     获取所有可行的出牌选项（包括跳过）。
    /// </summary>
    /// <param name="state">当前游戏状态</param>
    /// <returns>可行出牌项</returns>
    public static List<Card?> GetPossibleActions(State state)
    {
        var actions = new List<Card?>
        {
            null // 添加跳过选项
        };

        if (state.Hand.Count == 0 || state.Spaces <= 0) return actions;

        // 添加手牌选项（去重）
        var seen = new HashSet<(int, int)>();
        actions.AddRange(from card in state.Hand let key = (card.TopicID, card.Value) where seen.Add(key) select card);

        return actions;
    }

    /// <summary>
    ///     计算论题完成进度。
    /// </summary>
    /// <param name="topic">目标论题</param>
    /// <param name="table">桌面</param>
    /// <returns>论题在桌面上的完成进度</returns>
    private static double _CalculateTopicCompletionProgress(Topic topic, List<Card> table)
    {
        if (topic.Goals.Count == 0) return 1.0;

        // 全部换算成 Value = 0 的卡片数量作对比
        var currentWorth = table
            .Where(c => c.TopicID == topic.ID)
            .Select(c => Math.Pow(2, c.Value))
            .Sum();
        var goalWorth = topic.Goals
            .Select(g => Math.Pow(2, g))
            .Sum();

        return currentWorth / goalWorth;
    }
}