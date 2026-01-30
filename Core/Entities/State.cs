using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoLunDao.Core.Entities;

/// <summary>
///     当前游戏的状态。
/// </summary>
/// <param name="topics">论题</param>
/// <param name="hand">手牌</param>
/// <param name="table">桌面</param>
/// <param name="turnsLeft">剩余回合数</param>
/// <param name="spaces">桌面剩余空位</param>
/// <param name="originalTopics">游戏最开始时的论题</param>
public class State(
    List<Topic> topics,
    List<Card> hand,
    List<Card> table,
    int turnsLeft,
    int spaces,
    List<Topic>? originalTopics = null) : IEquatable<State>
{
    public static readonly State Invalid = new();

    public readonly List<Card> Hand = hand;

    public readonly bool IsInvalid;

    public readonly List<Topic> OriginalTopics =
        originalTopics ?? topics.Select(t => new Topic(t.ID, t.Goals.ToList())).ToList();

    public readonly int Spaces = spaces;
    public readonly List<Card> Table = table;
    public readonly List<Topic> Topics = topics;
    public readonly int TurnsLeft = turnsLeft;

    private State() : this([], [], [], 0, 0, [])
    {
        IsInvalid = true;
    }

    public bool Equals(State? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return TurnsLeft == other.TurnsLeft &&
               Spaces == other.Spaces &&
               Topics.SequenceEqual(other.Topics) &&
               Hand.SequenceEqual(other.Hand) &&
               Table.SequenceEqual(other.Table) &&
               OriginalTopics.SequenceEqual(other.OriginalTopics);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as State);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Topics.GetHashCode();
            hashCode = (hashCode * 397) ^ Hand.GetHashCode();
            hashCode = (hashCode * 397) ^ Table.GetHashCode();
            hashCode = (hashCode * 397) ^ TurnsLeft;
            hashCode = (hashCode * 397) ^ Spaces;
            hashCode = (hashCode * 397) ^ OriginalTopics.GetHashCode();
            return hashCode;
        }
    }

    public static bool operator ==(State? left, State? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(State? left, State? right)
    {
        return !(left == right);
    }
}