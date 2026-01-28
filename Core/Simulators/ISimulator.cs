using AutoLunDao.Core.Entities;

namespace AutoLunDao.Core.Simulators;

using Card = Card;

/// <summary>
///     模拟器接口。
///     用于模拟游戏出牌，返回出牌后的游戏状态，不会处理回合结束等事务，那是 ISandbox 的工作。
/// </summary>
public interface ISimulator
{
    /// <summary>
    ///     应用出牌效果，返回新的游戏状态。
    ///     我们假定返回的状态永远不是 <see cref="State.Invalid" />。
    /// </summary>
    /// <param name="state">当前游戏状态</param>
    /// <param name="card">要打出的手牌，null 表示不出</param>
    /// <returns>应用出牌效果后的新游戏状态</returns>
    State ApplyPlay(State state, Card? card);
}