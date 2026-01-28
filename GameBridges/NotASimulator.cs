using System;
using AutoLunDao.Core.Entities;
using AutoLunDao.Core.Simulators;

namespace AutoLunDao.GameBridges;

/// <summary>
///     并非是模拟器，通过游戏桥与游戏进行真实交互。
/// </summary>
/// <param name="bridge">使用的游戏桥</param>
public class NotASimulator(IGameBridge bridge) : ISimulator
{
    private readonly IGameBridge _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));

    public State ApplyPlay(State state, Core.Entities.Card? card)
    {
        if (card is null)
        {
            _bridge.ConfirmNoBestCard(out var err);
            if (err is not null)
                Plugin.Logger.LogError($"决策算法已确认无最佳出牌，{_bridge.Name}发送确认请求时失败：{err}");
        }
        else
        {
            _bridge.ConfirmBestCard(card.TopicID, card.Value, out var err);
            if (err is not null)
                Plugin.Logger.LogError($"决策算法已确定最佳出牌，{_bridge.Name}发送确认请求时失败：{err}");
        }

        var newState = _bridge.BuildStateSnapshot(out var error);
        if (error is null) return newState;

        Plugin.Logger.LogError("尝试读取游戏状态失败：" + error);
        return State.Invalid;
    }
}