using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoLunDao.Core.Entities;

/// <summary>
///     论题。
/// </summary>
/// <param name="id">论题标识</param>
/// <param name="goals">目标点数</param>
public class Topic(int id, List<int> goals) : IEquatable<Topic>
{
    public readonly List<int> Goals = goals;
    public readonly int ID = id;

    public bool Equals(Topic? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return ID == other.ID && Goals.SequenceEqual(other.Goals);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Topic);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (Goals.GetHashCode() * 397) ^ ID;
        }
    }

    public static bool operator ==(Topic? left, Topic? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(Topic? left, Topic? right)
    {
        return !(left == right);
    }
}