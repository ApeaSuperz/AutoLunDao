using System;
using System.Collections.Generic;
using System.Linq;
using AutoLunDao.Benchmarks.Sandboxes;
using AutoLunDao.Core.Entities;
using AutoLunDao.Core.Simulators;
using AutoLunDao.Core.Strategies;

namespace AutoLunDao.Benchmarks;

public class StrategyComparator(ISimulator simulator)
{
    public void CompareStrategies(
        IDecisionStrategy strategy1,
        IDecisionStrategy strategy2,
        int totalGames = 10000,
        int maxDifferences = 10)
    {
        Console.WriteLine($"对比策略: 【{strategy1.Name}】 vs 【{strategy2.Name}】");
        Console.WriteLine($"总测试场次: {totalGames}");
        Console.WriteLine(new string('═', 60));

        var differencesFound = 0;

        for (var seed = 0; seed < totalGames && differencesFound < maxDifferences; seed++)
        {
            var sandbox1 = new VanillaGameSandbox(seed, simulator);
            var sandbox2 = new VanillaGameSandbox(seed, simulator);

            while (sandbox1.CurrentState.TurnsLeft >= 0 && sandbox1.CurrentState.Topics.Count > 0)
                if (!sandbox1.StartNextTurn(strategy1))
                    break;

            while (sandbox2.CurrentState.TurnsLeft >= 0 && sandbox2.CurrentState.Topics.Count > 0)
                if (!sandbox2.StartNextTurn(strategy2))
                    break;

            if (sandbox1.CurrentState.Topics.Count == sandbox2.CurrentState.Topics.Count) continue;

            differencesFound++;
            var score1 = CalculateScore(sandbox1);
            var score2 = CalculateScore(sandbox2);
            PrintGameDifference(seed, sandbox1, sandbox2, strategy1.Name, strategy2.Name, score1, score2);
        }

        Console.WriteLine($"\n在 {totalGames} 场游戏中找到 {differencesFound} 场结果不同的游戏");
    }

    private static float CalculateScore(ISandbox sandbox)
    {
        var state = sandbox.CurrentState;
        var completedCount = sandbox.InitialTopics.Count - state.Topics.Count;
        return completedCount * 20f + (100f - state.Topics.Sum(t => t.Goals.Max()));
    }

    private static void PrintGameDifference(
        int seed,
        ISandbox sandbox1,
        ISandbox sandbox2,
        string name1,
        string name2,
        float score1,
        float score2)
    {
        Console.WriteLine($"\n【游戏 #{seed}】结果差异 (种子={seed})");
        Console.WriteLine(new string('─', 60));

        Console.WriteLine("📋 初始论题:");
        foreach (var topic in sandbox1.InitialTopics)
        {
            var goals = string.Join(", ", topic.Goals.OrderByDescending(g => g));
            Console.WriteLine($"   ID={topic.ID}, 目标=[{goals}]");
        }

        Console.WriteLine("\n⚔️ 最终结果:");
        Console.WriteLine($"   {name1}: 得分={score1:F2}, 剩余论题={GetTopicsString(sandbox1.CurrentState.Topics)}");
        Console.WriteLine($"   {name2}: 得分={score2:F2}, 剩余论题={GetTopicsString(sandbox2.CurrentState.Topics)}");

        Console.WriteLine($"\n🎴 {name1} 最终场上牌:");
        PrintTable(sandbox1.CurrentState.Table);

        Console.WriteLine($"\n🎴 {name2} 最终场上牌:");
        PrintTable(sandbox2.CurrentState.Table);

        Console.WriteLine($"\n🖐️ {name1} 最终手牌:");
        PrintHand(sandbox1.CurrentState.Hand);

        Console.WriteLine($"\n🖐️ {name2} 最终手牌:");
        PrintHand(sandbox2.CurrentState.Hand);

        Console.WriteLine(new string('─', 60));
    }

    private static void PrintTable(List<Card> table)
    {
        if (table.Count == 0)
        {
            Console.WriteLine("   (空)");
            return;
        }

        var grouped = table.GroupBy(c => c.TopicID).OrderBy(g => g.Key);
        foreach (var group in grouped)
        {
            var cards = string.Join(", ", group.OrderByDescending(c => c.Value).Select(c => c.Value));
            Console.WriteLine($"   论题{group.Key}: [{cards}]");
        }
    }

    private static string GetTopicsString(List<Topic> topics)
    {
        return $"({topics.Count}){{{string.Join(", ", topics.Select(t => t.ID))}}}";
    }

    private static void PrintHand(List<Card> hand)
    {
        if (hand.Count == 0)
        {
            Console.WriteLine("   (空)");
            return;
        }

        var grouped = hand.GroupBy(c => c.TopicID).OrderBy(g => g.Key);
        foreach (var group in grouped)
        {
            var cards = string.Join(", ", group.OrderByDescending(c => c.Value).Select(c => c.Value));
            Console.WriteLine($"   论题{group.Key}: [{cards}]");
        }
    }
}