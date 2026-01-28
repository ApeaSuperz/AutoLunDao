using System;
using System.Collections.Generic;
using System.Linq;
using AutoLunDao.Core.Entities;
using AutoLunDao.Core.Strategies;

namespace AutoLunDao.Core.Simulators;

using Card = Card;

/// <summary>
///     原版游戏沙盒环境，在内存中模拟游戏逻辑。
/// </summary>
public class VanillaGameSimulator : ISimulator
{
    public State ApplyPlay(State state, Card? card)
    {
        if (card is null) return state;

        if (!state.Hand.Contains(card))
            throw new InvalidOperationException($"无手牌 ({card.TopicID},{card.Value})");
        if (state.Spaces - 1 < 0)
            throw new InvalidOperationException("无空位");

        var hand = state.Hand.ToList();
        hand.Remove(card);
        var table = state.Table.ToList();
        table.Add(card);
        var topics = _RemoveCompletedTopics(state.Topics, table);
        table = StrategyUtils.MergeCardsOnTable(table);
        topics = _RemoveCompletedTopics(topics, table);

        var spacesChange = table.Count - state.Table.Count;
        return new State(topics, hand, table, state.TurnsLeft, state.Spaces - spacesChange);
    }

    private static List<Topic> _RemoveCompletedTopics(List<Topic> topics, List<Card> table)
    {
        return topics.Where(t => !StrategyUtils.IsTopicGoalsOnTable(table, t)).ToList();
    }
}