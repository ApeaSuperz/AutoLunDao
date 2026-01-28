using System;

namespace AutoLunDao.Core.Entities;

/// <summary>
///     手牌。
/// </summary>
/// <param name="topicId">论题标识</param>
/// <param name="value">卡牌点数</param>
public class Card(int topicId, int value) : IEquatable<Card>
{
    public readonly int TopicID = topicId;
    public readonly int Value = value;

    public bool Equals(Card? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return TopicID == other.TopicID && Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Card);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (TopicID * 397) ^ Value;
        }
    }

    public static bool operator ==(Card? left, Card? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(Card? left, Card? right)
    {
        return !(left == right);
    }
}