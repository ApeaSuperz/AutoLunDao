using System;
using System.Linq;
using AutoLunDao.Core.Strategies;
using AutoLunDao.Engine;
using AutoLunDao.GameBridges;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace AutoLunDao;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    public const string FullName =
        $"{MyPluginInfo.PLUGIN_NAME}（{MyPluginInfo.PLUGIN_GUID}）v{MyPluginInfo.PLUGIN_VERSION}";

    public new static ManualLogSource Logger { get; private set; }

    /// <summary>
    ///     插件总开关。
    /// </summary>
    public static ConfigEntry<bool> PluginEnabled { get; private set; }

    /// <summary>
    ///     是否自动结束回合。
    /// </summary>
    public static ConfigEntry<bool> AutoEndTurn { get; private set; }

    /// <summary>
    ///     是否自动选中最佳出牌。
    /// </summary>
    public static ConfigEntry<bool> AutoSelectBestCard { get; private set; }

    /// <summary>
    ///     是否启用全自动模式。
    /// </summary>
    public static ConfigEntry<bool> FullSelfPlaying { get; private set; }

    /// <summary>
    ///     前瞻策略的最大搜索深度。
    /// </summary>
    public static ConfigEntry<int> MaxLookaheadDepth { get; private set; }

    /// <summary>
    ///     选择的决策策略名称。
    /// </summary>
    public static ConfigEntry<string> StrategyName { get; private set; }

    /// <summary>
    ///     决策引擎实例。
    /// </summary>
    public static DecisionEngine Engine { get; private set; }

    private void Awake()
    {
        Logger = base.Logger;

        PluginEnabled = Config.Bind("通用", "总开关", true, $"启用{FullName}（本 Mod 所有设置均为即时生效，无需重启游戏）");
        AutoEndTurn = Config.Bind("通用", "自动结束回合", true, "在手牌打空与桌面无空位时自动结束回合");
        AutoSelectBestCard = Config.Bind("通用", "自动选中", true, "自动选中最佳出牌");
        FullSelfPlaying = Config.Bind("通用", "全自动模式", false, "无需手动确认出牌，无需手动结束回合");

        _InitEngineAndStrategies();

        try
        {
            Harmony.CreateAndPatchAll(typeof(Patches));
            Logger.LogInfo($"已加载插件{FullName}");
        }
        catch (Exception e)
        {
            Logger.LogError($"插件{FullName}加载失败：{e}");
        }
    }

    private void _InitEngineAndStrategies()
    {
        MaxLookaheadDepth = Config.Bind("前瞻策略", "最大搜索深度", 5,
            "前瞻策略在计算最佳出牌时的最大预判深度，数值越大计算时间越长，但决策可能越优。\n如果安装有增加手牌数量的模组，建议适当调高此数值以提升决策质量，如遇性能问题，可考虑更换为其他策略。");

        var lookaheadStrategy = new LookaheadStrategy(MaxLookaheadDepth.Value);

        // TODO: 检测其他 Mod 并提供相应的 Bridge
        Engine = new DecisionEngine(new VanillaGameBridge());
        Engine.Register(new BaselineStrategy());
        Engine.Register(new ImprovedBaselineStrategy());
        // Engine.Register(new GreedyStrategy());
        Engine.Register(lookaheadStrategy);

        StrategyName = Config.Bind("通用", "决策策略", lookaheadStrategy.Name,
            new ConfigDescription(
                string.Join("\n\n", Engine.Strategies.Select(kv => $"{kv.Key}：{kv.Value.Description}")),
                new AcceptableValueList<string>(Engine.Strategies.Keys.ToArray()))
        );

        // 响应最大搜索深度修改
        MaxLookaheadDepth.SettingChanged += (_, _) =>
        {
            Engine.Register(new LookaheadStrategy(MaxLookaheadDepth.Value));
        };
    }
}