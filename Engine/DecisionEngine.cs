using System.Collections.Generic;
using System.Linq;
using AutoLunDao.Core.Simulators;
using AutoLunDao.Core.Strategies;
using AutoLunDao.GameBridges;

namespace AutoLunDao.Engine;

public class DecisionEngine
{
    private readonly IGameBridge _bridge;
    private readonly ISimulator _executor;
    private readonly Dictionary<string, IDecisionStrategy> _registry = new();
    private IDecisionStrategy? _strategy;

    public DecisionEngine(IGameBridge bridge)
    {
        _bridge = bridge;

        // TODO: 检测其他 Mod 并提供相应的 Simulator
        _executor = new NotASimulator(_bridge);

        Plugin.Logger.LogInfo($"决策引擎已初始化，使用游戏桥：{bridge.Name}");
    }

    public Dictionary<string, IDecisionStrategy> Strategies => new(_registry);

    public void Register(IDecisionStrategy? strategy)
    {
        if (strategy == null) return;

        // 如果当前策略与新注册的策略同名，则更新当前策略引用
        if (_strategy is not null && _strategy.Name == strategy.Name)
            _strategy = strategy;

        _registry[strategy.Name] = strategy;
    }

    /// <summary>
    ///     执行论道决策循环。
    /// </summary>
    /// <param name="triggeredByPlayerPlay">本次调用是否由 <see cref="PlayerController.PlayerUseCard" /> 方法引起</param>
    public void ExecuteDecisionLoop(bool triggeredByPlayerPlay)
    {
        if (!Plugin.PluginEnabled.Value) return;

        var isPlayerTurn = _bridge.IsPlayerTurn(out var turnError);
        if (turnError is not null)
        {
            Plugin.Logger.LogError($"决策引擎已收到决策指令，但尝试判断是否玩家回合失败：{turnError}");
            return;
        }

        if (!isPlayerTurn) return;

        var state = _bridge.BuildStateSnapshot(out var error);
        if (error is not null)
        {
            Plugin.Logger.LogError($"决策引擎已收到决策指令，但尝试读取游戏状态失败：{error}");
            return;
        }

        // 如果状态无效则不进行决策，状态无效一般是因为读取状态时由于用户设置自动跳过了当前用户出牌回合，无需再进行决策
        if (state.IsInvalid) return;

        _EnsureDecisionStrategy();
        var best = _strategy?.Decide(state, new VanillaGameSimulator());
        _executor.ApplyPlay(state, best);
    }

    private bool _Select(string name)
    {
        return _registry.TryGetValue(name, out var s) && (_strategy = s) != null;
    }

    private void _EnsureDecisionStrategy()
    {
        if (!_Select(Plugin.StrategyName.Value))
            _Select(_registry.First().Key);
        if (_strategy is null)
            Plugin.Logger.LogError($"无法选择决策策略，当前注册的策略列表为空，设置的策略名称为：{Plugin.StrategyName.Value}");
    }
}