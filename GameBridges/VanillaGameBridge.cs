using System.Collections.Generic;
using System.Linq;
using AutoLunDao.Core.Entities;
using AutoLunDao.UI;

namespace AutoLunDao.GameBridges;

using Card = Core.Entities.Card;

/// <summary>
///     游戏适配器的默认实现，直接与原版游戏交互，不考虑其它 Mod 的影响。
/// </summary>
public class VanillaGameBridge : IGameBridge
{
    public string Name => "原版游戏桥";

    public bool ConfirmBestCard(int topicId, int value, out string? error)
    {
        var manager = LunDaoManager.inst;
        if (manager == null)
        {
            error = "LunDaoManager 为 null 或已销毁";
            return false;
        }

        var player = manager.playerController;
        if (player == null)
        {
            error = "PlayerController 为 null 或已销毁";
            return false;
        }

        if (!Plugin.FullSelfPlaying.Value)
            EnsureBadgesInit(player.cards);

        var bestCards = player.cards
            ?.Where(c => c.lunDaoCard.wudaoId == topicId && c.lunDaoCard.level == value)
            .ToList();

        if (bestCards is null)
        {
            error = "无法获取玩家手牌列表";
            return false;
        }

        if (bestCards.Count == 0)
        {
            error = "未在玩家手牌中找到指定卡牌";
            return false;
        }

        error = null;
        PlayOrAdvise(player, bestCards);
        return true;
    }

    public State BuildStateSnapshot(out string? error)
    {
        error = null;

        var manager = LunDaoManager.inst;
        if (manager == null)
        {
            error = "LunDaoManager 为 null 或已销毁";
            return State.Invalid;
        }

        var player = manager.playerController;
        if (player == null)
        {
            error = "PlayerController 为 null 或已销毁";
            return State.Invalid;
        }

        // 计算桌面剩余空位
        var spaces = manager.lunTiMag?.curLunDianList?.Count(slot => slot.isNull) ?? 0;
        if (spaces <= 0 && EndTurnIfAutoEndEnabled(player)) return State.Invalid;

        // 读取玩家手牌
        var hand = player.cards
            ?.Select(c => new Card(c.lunDaoCard.wudaoId, c.lunDaoCard.level))
            .ToList() ?? [];
        if (hand.Count == 0 && EndTurnIfAutoEndEnabled(player)) return State.Invalid;

        // 读取论题信息
        var topics = manager.lunTiMag?.targetLunTiDictionary
            ?.Select(kv => new Topic(kv.Key, kv.Value.ToList()))
            .ToList();
        if (topics is null || topics.Count == 0)
        {
            if (topics == null) error = "无法读取论题信息";
            return State.Invalid;
        }

        // 读取桌面信息
        var table = manager.lunTiMag?.curLunDianList
            ?.Where(slot => !slot.isNull) // 过滤空位，空槽位是算一种卡片的，wudaoId 是 -1，干扰到合成逻辑了
            .Select(c => new Card(c.wudaoId, c.level))
            .ToList();
        if (table is null)
        {
            error = "无法读取桌面信息";
            return State.Invalid;
        }

        // 读取剩余回合数
        var turnsLeft = player.lunDaoHuiHe.shengYuHuiHe;

        return new State(topics, hand, table, turnsLeft, spaces);
    }

    public bool ConfirmNoBestCard(out string? error)
    {
        var manager = LunDaoManager.inst;
        if (manager == null)
        {
            error = "LunDaoManager 实例为 null 或已销毁";
            return false;
        }

        var player = manager.playerController;
        if (player == null)
        {
            error = "PlayerController 为 null 或已销毁";
            return false;
        }

        if (!Plugin.FullSelfPlaying.Value)
        {
            EnsureBadgesInit(player.cards);
            UIPopTip.Inst.Pop("无高收益手牌，建议结束本回合");
        }

        error = null;
        return EndTurnIfFullAutoEnabled(player);
    }

    public bool IsPlayerTurn(out string? error)
    {
        var manager = LunDaoManager.inst;
        if (manager == null)
        {
            error = "LunDaoManager 实例为 null 或已销毁";
            return false;
        }

        error = null;
        return manager.gameState == LunDaoManager.GameState.玩家回合;
    }

    private static bool EndTurnIfAutoEndEnabled(PlayerController player)
    {
        if (!Plugin.AutoEndTurn.Value) return EndTurnIfFullAutoEnabled(player);
        player.PlayerEndRound();
        return true;
    }

    private static bool EndTurnIfFullAutoEnabled(PlayerController player)
    {
        if (!Plugin.FullSelfPlaying.Value) return false;
        player.PlayerEndRound();
        return true;
    }

    private static void EnsureBadgesInit(List<LunDaoPlayerCard>? cards)
    {
        if (cards is null) return;

        foreach (var card in cards)
        {
            var badge = card.cardImage.gameObject.GetComponent<CardScoreBadge>();
            if (badge is null)
            {
                badge = card.cardImage.gameObject.AddComponent<CardScoreBadge>();
                badge.Initialize(card);
            }

            badge.ResetScore();
        }
    }

    private static void MarkCardAsBest(LunDaoPlayerCard card)
    {
        var badge = card.cardImage.gameObject.GetComponent<CardScoreBadge>();
        if (badge is null)
        {
            badge = card.cardImage.gameObject.AddComponent<CardScoreBadge>();
            badge.Initialize(card);
        }

        badge.MarkAsBest();
    }

    private static void PlayOrAdvise(PlayerController player, List<LunDaoPlayerCard> cards)
    {
        if (Plugin.FullSelfPlaying.Value)
        {
            // 全自动
            cards.First().SelectCard();
            player.Invoke(nameof(PlayerController.PlayerUseCard), 0.5f);
        }
        else
        {
            // 建议模式
            cards.ForEach(MarkCardAsBest);
            if (Plugin.AutoSelectBestCard.Value)
                cards.First().SelectCard();
        }
    }
}