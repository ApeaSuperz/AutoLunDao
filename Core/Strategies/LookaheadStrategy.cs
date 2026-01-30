using System;
using System.Linq;
using AutoLunDao.Core.Entities;
using AutoLunDao.Core.Simulators;

namespace AutoLunDao.Core.Strategies;

/// <summary>
///     前瞻策略：使用有限深度搜索评估未来状态。
/// </summary>
public class LookaheadStrategy(int maxDepth = 5) : IDecisionStrategy
{
    public string Name => "【推荐】深度受限前瞻";
    public string Description => "【推荐】【速度：-134.52%】【得分：+37.65%】数据来源：最大搜索深度 =5 时。使用有限深度搜索评估未来状态，选择最优出牌";

    public Card? Decide(State state, ISimulator simulator)
    {
        if (state.Hand.Count == 0) return null;
        if (state.Spaces <= 0) return null;

        var actions = StrategyUtils.GetPossibleActions(state);

        Card? bestCard = null;
        var bestScore = 0f; // 初始分数为 0 而不是 float.MinValue，不打无意义的零分出牌
        foreach (var card in actions)
        {
            var score = SimulatePlay(state, simulator, card, maxDepth);
            if (score <= bestScore) continue;
            bestScore = score;
            bestCard = card;
        }

        return bestCard;
    }

    private static float SimulatePlay(State state, ISimulator simulator, Card? card, int depth)
    {
        if (depth <= 0 || card is null || state.Spaces <= 0) return 0f;

        var simState = simulator.ApplyPlay(state, card);
        var score = StrategyUtils.EvaluateStateChanges(state, simState);

        var nextActions = StrategyUtils.GetPossibleActions(simState);
        if (nextActions.Count <= 1) return score; // 首个动作是 null（跳过）

        var bestFutureScore = nextActions
            .OfType<Card>()
            .Where(c => !StrategyUtils.WillCauseMissOfExistingMaxGoal(c, simState))
            .Select(c => SimulatePlay(simState, simulator, c, depth - 1))
            .DefaultIfEmpty(0f)
            .Max();

        return score + bestFutureScore * 0.8f;
    }
}