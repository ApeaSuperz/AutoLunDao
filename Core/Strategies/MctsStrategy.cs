using System;
using System.Collections.Generic;
using System.Linq;
using AutoLunDao.Core.Entities;
using AutoLunDao.Core.Simulators;

namespace AutoLunDao.Core.Strategies;

/// <summary>
///     蒙特卡洛树搜索策略。
/// </summary>
/// <param name="iterations">迭代次数，默认为 1000。</param>
/// <param name="explorationConstant">探索常数，用于平衡探索与利用，默认为 1.41（√2）。</param>
public class MctsStrategy(int iterations = 1000, double explorationConstant = 1.41) : IDecisionStrategy
{
    private static readonly Random Rng = new();

    public string Name => "【慎选】蒙特卡洛树搜索";
    public string Description => "【慎选】【速度：-68,431.90%】【得分：-3.04%】使用蒙特卡洛树搜索评估未来状态，选择最优出牌，原本寄予厚望，但在本游戏的论道规则中表现并不理想";

    public Card? Decide(State state, ISimulator simulator)
    {
        if (state.Hand.Count == 0) return null;
        if (state.Spaces <= 0) return null;

        var root = new MctsNode(state, null, null);
        var controlTopics = state.Topics.ToList();

        for (var i = 0; i < iterations; i++)
        {
            var node = Select(root);
            node = Expand(node, simulator);
            var score = Simulate(node.State, simulator, controlTopics);
            Backpropagate(node, score);
        }

        var bestChild = root.Children
            .Where(c => c.Action is not null)
            .OrderByDescending(c => c.Visits)
            .FirstOrDefault();

        return bestChild?.Action;
    }

    /// <summary>
    ///     选择阶段：从根节点向下选择最优子节点。
    /// </summary>
    private MctsNode Select(MctsNode node)
    {
        while (node.Children.Count > 0 && node.IsFullyExpanded)
            node = node.Children
                .OrderByDescending(CalculateUcb)
                .First();

        return node;
    }

    /// <summary>
    ///     计算 UCB（上置信界）值。
    /// </summary>
    private double CalculateUcb(MctsNode node)
    {
        if (node.Visits == 0) return double.MaxValue;

        var exploitation = node.TotalScore / node.Visits;
        var exploration = explorationConstant * Math.Sqrt(Math.Log(node.Parent!.Visits) / node.Visits);

        return exploitation + exploration;
    }

    /// <summary>
    ///     扩展阶段：为当前节点添加一个新的子节点。
    /// </summary>
    private static MctsNode Expand(MctsNode node, ISimulator simulator)
    {
        if (node.State.TurnsLeft < 0 || node.State.Topics.Count == 0)
            return node;

        var actions = StrategyUtils.GetPossibleActions(node.State);
        var unexpandedActions = actions
            .Where(a => node.Children.All(c => !ReferenceEquals(c.Action, a) && c.Action is null != a is null))
            .ToList();

        if (unexpandedActions.Count == 0)
        {
            node.IsFullyExpanded = true;
            return node;
        }

        var action = unexpandedActions[Rng.Next(unexpandedActions.Count)];

        State newState;
        if (action is null)
            newState = node.State;
        else
            try
            {
                newState = simulator.ApplyPlay(node.State, action);
            }
            catch
            {
                return node;
            }

        var child = new MctsNode(newState, action, node);
        node.Children.Add(child);

        if (node.Children.Count >= actions.Count)
            node.IsFullyExpanded = true;

        return child;
    }

    /// <summary>
    ///     模拟阶段：从当前状态随机模拟直到终局。
    /// </summary>
    private static float Simulate(State state, ISimulator simulator, List<Topic> controlTopics)
    {
        var simState = StrategyUtils.CreateStateCopy(state);
        var depth = 0;
        const int maxSimulationDepth = 20;

        while (depth < maxSimulationDepth && simState.Topics.Count > 0 && simState.TurnsLeft >= 0)
        {
            var actions = StrategyUtils.GetPossibleActions(simState);
            if (actions.Count == 0) break;

            var action = actions[Rng.Next(actions.Count)];

            if (action is null)
                break;

            try
            {
                simState = simulator.ApplyPlay(simState, action);
            }
            catch
            {
                break;
            }

            depth++;
        }

        return StrategyUtils.EvaluateState(simState, controlTopics);
    }

    /// <summary>
    ///     反向传播阶段：将模拟结果向上传播到所有祖先节点。
    /// </summary>
    private static void Backpropagate(MctsNode? node, float score)
    {
        while (node is not null)
        {
            node.Visits++;
            node.TotalScore += score;
            node = node.Parent;
        }
    }

    /// <summary>
    ///     蒙特卡洛树搜索节点。
    /// </summary>
    private class MctsNode(State state, Card? action, MctsNode? parent)
    {
        public State State { get; } = state;
        public Card? Action { get; } = action;
        public MctsNode? Parent { get; } = parent;
        public List<MctsNode> Children { get; } = [];

        public int Visits { get; set; }
        public float TotalScore { get; set; }
        public bool IsFullyExpanded { get; set; }
    }
}