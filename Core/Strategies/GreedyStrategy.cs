using AutoLunDao.Core.Entities;
using AutoLunDao.Core.Simulators;

namespace AutoLunDao.Core.Strategies;

/// <summary>
///     贪心策略：选择能立即最大化当前评分的出牌。
/// </summary>
public class GreedyStrategy : IDecisionStrategy
{
    public string Name => "启发式贪心";
    public string Description => "【速度：-29.52%】【得分：+0.23%】选择能立即最大化当前收益的牌";

    public Card? Decide(State state, ISimulator simulator)
    {
        // 无手牌
        if (state.Hand.Count == 0) return null;

        // 无空位
        if (state.Spaces <= 0) return null;

        Card? bestCard = null;
        var bestScore = 0f;

        var actions = StrategyUtils.GetPossibleActions(state);

        foreach (var card in actions)
        {
            if (card is null) continue;

            // 检查是否会导致最大目标丢失
            if (StrategyUtils.WillCauseMissOfGoal(card, state))
                continue;

            // 模拟出牌
            var simState = simulator.ApplyPlay(state, card);

            var score = StrategyUtils.EvaluateStateChanges(state, simState);
            if (score <= bestScore) continue;

            bestScore = score;
            bestCard = card;
        }

        return bestCard;
    }
}