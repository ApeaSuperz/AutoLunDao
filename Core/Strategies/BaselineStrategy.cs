using System.Linq;
using AutoLunDao.Core.Entities;
using AutoLunDao.Core.Simulators;

namespace AutoLunDao.Core.Strategies;

/// <summary>
///     基线策略：模拟原版 NPC 的出牌逻辑，用于基准测试，作为评估其他策略的标准。
/// </summary>
public class BaselineStrategy : IDecisionStrategy
{
    public string Name => "基线（仿 NPC）";
    public string Description => "【速度：+0% (基线)】【得分：+0% (基线)】模拟原版 NPC 的出牌逻辑，本来只是想用于基准测试，作为评估其它策略的标准，没想到效果还行";

    public Card? Decide(State state, ISimulator simulator)
    {
        if (state.Hand.Count == 0) return null;
        if (state.Spaces <= 0) return null;

        var actions = StrategyUtils.GetPossibleActions(state);

        // 1. 能直接完成论题的牌
        foreach (var card in from card in actions
                 let simulated = simulator.ApplyPlay(state, card)
                 where simulated.Topics.Count != state.Topics.Count
                 select card)
            return card;

        // 2. 完成还未完成的论题，最高点数的牌优先
        return state.Topics
            .Select(topic =>
            {
                var maxGoal = topic.Goals.Max();
                return actions
                    .Where(c => c?.TopicID == topic.ID && c.Value <= maxGoal)
                    .OrderByDescending(c => c!.Value)
                    .FirstOrDefault();
            })
            .OfType<Card>()
            .OrderByDescending(c => c.Value)
            .FirstOrDefault();
    }
}