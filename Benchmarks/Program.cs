using System;
using AutoLunDao.Benchmarks;
using AutoLunDao.Core.Simulators;
using AutoLunDao.Core.Strategies;

var evaluator = new StrategyEvaluator(new VanillaGameSimulator());
const int gameCount = 1000;

var strategies = new IDecisionStrategy[]
{
    new BaselineStrategy(),
    new ImprovedBaselineStrategy(),
    new GreedyStrategy(),
    new LookaheadStrategy()
    // new MctsStrategy()
};

Console.WriteLine("╔══════════════════════════════════════════════════════════════════╗");
Console.WriteLine("║                    策略评估报告                                  ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════════════╝");
Console.WriteLine();

foreach (var strategy in strategies)
{
    var name = strategy.GetType().Name;
    var result = evaluator.Evaluate(strategy, gameCount);

    Console.WriteLine($"【{name}】");
    Console.WriteLine($"  游戏场次: {result.GameCount}");
    Console.WriteLine($"  胜利场次: {result.Wins}");
    Console.WriteLine($"  胜    率: {result.WinRate:P2}");
    Console.WriteLine($"  平均得分: {result.AveragePoints:F2} (范围: {result.MinPoints:F2} ~ {result.MaxPoints:F2})");
    Console.WriteLine($"  平均回合: {result.AverageTurns:F1} (范围: {result.MinTurns} ~ {result.MaxTurns})");
    Console.WriteLine($"  耗时: {result.TotalTime.TotalMilliseconds:F0}ms");
    Console.WriteLine(new string('─', 50));
}

// var simulator = new VanillaGameSimulator();
// var comparator = new StrategyComparator(simulator);
//
// comparator.CompareStrategies(
//     new BaselineStrategy(),
//     new ImprovedBaselineStrategy(),
//     totalGames: 1000,
//     maxDifferences: 5
// );