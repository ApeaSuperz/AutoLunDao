using AutoLunDao.Core.Entities;

namespace AutoLunDao.GameBridges;

/// <summary>
///     游戏桥接器接口，封装与觅长生的交互。
/// </summary>
public interface IGameBridge
{
    /// <summary>
    ///     游戏桥名称。
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     确定最佳出牌，具体完成的操作取决于插件的设置。
    /// </summary>
    /// <param name="topicId">最佳出牌的论题 ID</param>
    /// <param name="value">最佳出牌的卡牌点数</param>
    /// <param name="error">错误信息，无错误则为 null</param>
    /// <returns>是否对游戏进行了操作</returns>
    bool ConfirmBestCard(int topicId, int value, out string? error);

    /// <summary>
    ///     确定无最佳出牌，具体完成的操作取决于插件的设置。
    /// </summary>
    /// <param name="error">错误信息，无错误则为 null</param>
    /// <returns>是否对游戏进行了操作</returns>
    bool ConfirmNoBestCard(out string? error);

    /// <summary>
    ///     读取当前游戏状态，并封装为本 Mod 内部使用的 <see cref="State" /> 对象。
    ///     读取过程可能会操作游戏，取决于插件的设置。
    /// </summary>
    /// <param name="error">错误信息，无错误则为 null</param>
    /// <returns>调用该方法时的游戏状态</returns>
    State BuildStateSnapshot(out string? error);

    /// <summary>
    ///     判断当前是否为玩家回合。
    /// </summary>
    /// <param name="error">错误信息，无错误则为 null</param>
    /// <returns>是否为玩家回合</returns>
    bool IsPlayerTurn(out string? error);
}