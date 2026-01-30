using System.Diagnostics.CodeAnalysis;
using HarmonyLib;

namespace AutoLunDao;

/// <summary>
///     Harmony 补丁。
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class Patches
{
    /// <summary>
    ///     论道游戏开始时触发。
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.PlayerStartRound))]
    public static void OnLunDaoStarted(PlayerController __instance)
    {
        // 在论道开始时执行决策循环，以便在玩家回合一开始就给出建议或自动出牌
        Plugin.Engine.ExecuteDecisionLoop(false);
    }

    /// <summary>
    ///     玩家出牌后触发（包括模组帮玩家出牌）。
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.PlayerUseCard))]
    public static void OnPlayerPlayedCard(PlayerController __instance)
    {
        // 在玩家出牌后执行决策循环，以便给出新的建议或继续自动出牌
        Plugin.Engine.ExecuteDecisionLoop(true);
    }
}