using AutoLunDao.Core.Entities;
using AutoLunDao.Core.Simulators;

namespace AutoLunDao.Core.Strategies;

/// <summary>
///     决策策略接口。
/// </summary>
public interface IDecisionStrategy
{
    /// <summary>
    ///     策略名称。
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     决策说明。
    /// </summary>
    string Description { get; }

    /// <summary>
    ///     返回要出的卡片，null 表示跳过。
    /// </summary>
    /// <param name="state">当前游戏状态</param>
    /// <param name="simulator">沙盒环境</param>
    /// <returns>策略决定的最佳出牌</returns>
    Card? Decide(State state, ISimulator simulator);
}