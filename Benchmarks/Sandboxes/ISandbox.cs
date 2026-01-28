using System;
using System.Collections.Generic;
using AutoLunDao.Core.Entities;
using AutoLunDao.Core.Strategies;

namespace AutoLunDao.Benchmarks.Sandboxes;

/// <summary>
///     沙盒环境接口。
///     沙盒环境用于模拟游戏的进行，与 <see cref="LunDaoManager" /> 的功能类似。
/// </summary>
public interface ISandbox : ICloneable
{
    /// <summary>
    ///     沙盒环境中当前的游戏状态。
    ///     <see cref="State.Hand" /> 不应作为评估指标，它可能是玩家的手牌，也可能是 NPC 的手牌，取决于当前的 <see cref="GameStatus" />。
    ///     有评估价值的数据应该是 <see cref="State.Topics" /> 和 <see cref="State.TurnsLeft" />。
    /// </summary>
    State CurrentState { get; }

    /// <summary>
    ///     游戏刚开始时的初始论题列表。
    /// </summary>
    List<Topic> InitialTopics { get; }

    /// <summary>
    ///     在沙盒环境中开始下一回合。
    /// </summary>
    /// <param name="strategy">要用于玩家回合的决策策略</param>
    /// <returns>是否成功进行了下一回合</returns>
    bool StartNextTurn(IDecisionStrategy strategy);
}