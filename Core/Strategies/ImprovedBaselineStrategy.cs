using System.Linq;
using AutoLunDao.Core.Entities;
using AutoLunDao.Core.Simulators;

namespace AutoLunDao.Core.Strategies;

/// <summary>
///     改进基准策略：模仿 NPC 出牌规则，但优先打出可合成卡牌，并使用轻量前瞻打破平局。
/// </summary>
public class ImprovedBaselineStrategy : IDecisionStrategy
{
    public string Name => "改进基准（NPC 改良）";
    public string Description => "【速度：+10%】【得分：+7.41%】模仿 NPC 出牌规则，但优先打出可合成，轻量前瞻用于打破平局";

    public Card? Decide(State state, ISimulator simulator)
    {
        if (state.Hand.Count == 0) return null;
        if (state.Spaces <= 0) return null;

        var actions = StrategyUtils.GetPossibleActions(state);

        // 规则 1：能直接合成场上卡牌（仅限最后一回合的已完成论题）
        if (state.TurnsLeft == 0)
        {
            var mergeable = actions
                .OfType<Card>()
                .Where(c => state.Topics.All(t => t.ID != c.TopicID))
                .Where(c => state.Table.Contains(c))
                .OrderByDescending(c => c.Value)
                .FirstOrDefault();
            if (mergeable is not null) return mergeable;
        }

        // 规则 2：直接完成论题（与 Baseline 相同）
        foreach (var card in from card in actions
                 let simulated = simulator.ApplyPlay(state, card)
                 where simulated.Topics.Count < state.Topics.Count
                 select card)
            return card;

        // 规则 3：按 Baseline 逻辑选择候选牌
        var candidates = state.Topics
            .Select(topic =>
            {
                var maxGoal = topic.Goals.Max();
                return actions
                    .Where(c => c?.TopicID == topic.ID && c.Value <= maxGoal)
                    .OrderByDescending(c => c!.Value)
                    .FirstOrDefault();
            })
            .OfType<Card>()
            .ToList();

        switch (candidates.Count)
        {
            case 0:
                return null;
            case 1:
                return candidates[0];
        }

        // 规则3：多个候选时，用前瞻打破平局
        var maxValue = candidates.Max(c => c!.Value);
        var topCandidates = candidates.Where(c => c!.Value == maxValue).ToList();

        if (topCandidates.Count == 1)
            return topCandidates[0];

        // 前瞻选择
        return topCandidates
            .OrderByDescending(c => EvaluateFuture(state, simulator, c, 3))
            .First();
    }

    private static float EvaluateFuture(State state, ISimulator simulator, Card? card, int depth)
    {
        if (depth <= 0 || card is null) return 0f;

        var simState = simulator.ApplyPlay(state, card);
        var topicsCompleted = state.Topics.Count - simState.Topics.Count;

        if (topicsCompleted > 0) return 100f * topicsCompleted;

        var nextActions = StrategyUtils.GetPossibleActions(simState);
        if (nextActions.Count == 0) return 0f;

        return nextActions
            .Select(c => EvaluateFuture(simState, simulator, c, depth - 1))
            .DefaultIfEmpty(0f)
            .Max() * 0.9f;
    }
}