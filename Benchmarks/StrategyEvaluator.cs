using System;
using System.Diagnostics;
using AutoLunDao.Benchmarks.Sandboxes;
using AutoLunDao.Core.Entities;
using AutoLunDao.Core.Simulators;
using AutoLunDao.Core.Strategies;

namespace AutoLunDao.Benchmarks;

public record EvaluationResult(
    int GameCount,
    int Wins,
    double WinRate,
    double AveragePoints,
    double AverageTurns,
    double MinPoints,
    double MaxPoints,
    int MinTurns,
    int MaxTurns,
    TimeSpan TotalTime
)
{
    public int GameCount { get; } = GameCount;
    public int Wins { get; } = Wins;
    public double WinRate { get; } = WinRate;
    public double AveragePoints { get; } = AveragePoints;
    public double AverageTurns { get; } = AverageTurns;
    public double MinPoints { get; } = MinPoints;
    public double MaxPoints { get; } = MaxPoints;
    public int MinTurns { get; } = MinTurns;
    public int MaxTurns { get; } = MaxTurns;
    public TimeSpan TotalTime { get; } = TotalTime;
}

public class StrategyEvaluator(ISimulator simulator)
{
    public EvaluationResult Evaluate(IDecisionStrategy strategy, int gameCount)
    {
        var wins = 0;
        var points = 0;
        var totalTurns = 0;

        var minPoints = int.MaxValue;
        var maxPoint = int.MinValue;
        var minTurns = int.MaxValue;
        var maxTurns = int.MinValue;

        var stopwatch = Stopwatch.StartNew();

        for (var times = 0; times < gameCount; times++)
        {
            var sandbox = new VanillaGameSandbox(times, simulator);
            var turns = 0;

            while (sandbox.StartNextTurn(strategy))
                turns++;

            if (IsWin(sandbox.CurrentState))
                wins++;

            points += sandbox.Points;
            totalTurns += turns;

            minPoints = Math.Min(minPoints, sandbox.Points);
            maxPoint = Math.Max(maxPoint, sandbox.Points);
            minTurns = Math.Min(minTurns, turns);
            maxTurns = Math.Max(maxTurns, turns);
        }

        stopwatch.Stop();

        return new EvaluationResult(
            gameCount,
            wins,
            (double)wins / gameCount,
            (double)points / gameCount,
            (double)totalTurns / gameCount,
            minPoints,
            maxPoint,
            minTurns,
            maxTurns,
            stopwatch.Elapsed
        );
    }

    private static bool IsWin(State state)
    {
        return state.Topics.Count == 0;
    }
}