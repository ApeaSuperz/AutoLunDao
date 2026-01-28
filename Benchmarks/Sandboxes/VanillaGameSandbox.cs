using System;
using System.Collections.Generic;
using System.Linq;
using AutoLunDao.Core.Entities;
using AutoLunDao.Core.Simulators;
using AutoLunDao.Core.Strategies;

namespace AutoLunDao.Benchmarks.Sandboxes;

/// <summary>
///     模拟原版论道规则的沙盒。
/// </summary>
public class VanillaGameSandbox : ISandbox
{
    private const int MaxPlayerLevel = 5;
    private const int MaxNpcLevel = 5;

    private static readonly int[] Rewards =
        [30, 120, 240, 480, 960, 1920, 4000, 8000, 20000, 20000, 20000, 20000, 20000];

    private static readonly int[] LevelPointsReduce = [100, 86, 73, 56, 40, 23, 6, 0, 0];
    private static readonly int[] GoalPoints = [75, 375, 1500, 3000, 6000, 12000, 30000];

    private readonly List<Topic> _initialTopics;
    private readonly List<Card> _npcDeck;
    private readonly Dictionary<int, int> _npcLevels = new();
    private readonly List<Card> _playerDeck;
    private readonly Dictionary<int, int> _playerLevels = new();
    private readonly Random _random;

    private readonly int _seed;

    private readonly ISimulator _simulator;
    private int _randomCallCount; // 用于跟踪随机数调用次数，确保克隆时状态一致
    private State _state;

    public VanillaGameSandbox(int seed, ISimulator simulator)
    {
        _seed = seed;
        _random = new Random(seed);

        _initialTopics = [];
        _playerDeck = [];
        _npcDeck = [];

        _simulator = simulator;

        _InitializeRandomData();
    }

    private VanillaGameSandbox(VanillaGameSandbox original)
    {
        _seed = original._seed;
        _random = new Random(_seed);
        _randomCallCount = original._randomCallCount;
        for (var i = 0; i < original._randomCallCount; i++)
            _random.Next();

        _initialTopics = original._initialTopics.ToList();
        _playerDeck = original._playerDeck.ToList();
        _npcDeck = original._npcDeck.ToList();

        _simulator = original._simulator;
        _state = StrategyUtils.CreateStateCopy(original._state);

        _playerLevels = original._playerLevels;
        _npcLevels = original._npcLevels;
        Points = original.Points;
    }

    public int Points { get; private set; }

    public State CurrentState => StrategyUtils.CreateStateCopy(_state);
    public List<Topic> InitialTopics => _initialTopics.ToList();

    public bool StartNextTurn(IDecisionStrategy strategy)
    {
        // 无回合数剩余，游戏结束
        if (_state.TurnsLeft - 1 < 0) return false;

        // 论题已完成，游戏结束
        if (_state.Topics.Count == 0) return false;

        _playerDeck.AddRange(_state.Hand);

        var state = _SimulateNpcTurn();
        _state = new State(state.Topics, _DealPlayerCards(), state.Table, _state.TurnsLeft - 1, _state.Spaces + 1);

        Card? decision;
        do
        {
            var oldState = _state;
            decision = strategy.Decide(_state, _simulator);
            _state = _simulator.ApplyPlay(_state, decision);
            _GiveExp(oldState, state);
        } while (decision is not null);

        return true;
    }

    public object Clone()
    {
        return new VanillaGameSandbox(this);
    }

    private void _InitializeRandomData()
    {
        // 随机论题数量，1 到 4 个（一般来讲不考虑 4 个以上论题，几乎不可能完成）
        var topicsCount = _random.Next(1, 5);
        _randomCallCount += 1;

        for (var i = 0; i < topicsCount; i++)
        {
            // 为玩家和 NPC 随机生成悟道等级（悟道等级决定论道目标点数和手牌最大点数）
            var playerTopicLevel = _random.Next(1, MaxPlayerLevel + 1);
            var npcTopicLevel = _random.Next(1, MaxNpcLevel + 1);
            _randomCallCount += 2;

            _playerLevels[i] = playerTopicLevel;
            _npcLevels[i] = npcTopicLevel;

            // 根据悟道等级生成论题目标
            List<int> goals = playerTopicLevel == npcTopicLevel
                ? [playerTopicLevel + 2]
                : [playerTopicLevel + 1, npcTopicLevel + 1];
            _initialTopics.Add(new Topic(i, goals));

            // 添加相应卡牌到玩家和 NPC 的牌库
            for (var level = 1; level <= 5; level++)
            for (var copy = 1; copy <= 3; copy++)
            {
                _playerDeck.Add(new Card(i, playerTopicLevel - level >= 0 ? level : 0));
                _npcDeck.Add(new Card(i, npcTopicLevel - level >= 0 ? level : 0));
            }
        }

        _state = new State(_initialTopics.ToList(), [], [], 5, 0);
    }

    private List<Card> _DealNpcCards()
    {
        if (_npcDeck.Count < 5) return [];

        var cards = new List<Card>();
        for (var i = 0; i < 5; i++)
        {
            var index = _random.Next(0, _npcDeck.Count);
            _randomCallCount += 1;

            cards.Add(_npcDeck[index]);
            _npcDeck.RemoveAt(index);
        }

        return cards;
    }

    private List<Card> _DealPlayerCards()
    {
        var cards = new List<Card>();
        var limit = Math.Min(5, _playerDeck.Count);
        for (var i = 0; i < limit; i++)
        {
            var index = _random.Next(0, _playerDeck.Count);
            _randomCallCount += 1;

            cards.Add(_playerDeck[index]);
            _playerDeck.RemoveAt(index);
        }

        return cards;
    }

    private State _SimulateNpcTurn()
    {
        var state = new State(_state.Topics, _DealNpcCards(), _state.Table, _state.TurnsLeft, _state.Spaces + 1);
        var npc = new BaselineStrategy();

        Card? decision;
        do
        {
            var oldState = state;
            decision = npc.Decide(state, _simulator);
            state = _simulator.ApplyPlay(state, decision);
            _GiveExp(oldState, state);
        } while (decision is not null);

        _npcDeck.AddRange(state.Hand);

        return state;
    }

    private int _CalculateExp(int topicId, int num)
    {
        var playerTopicLevel = _playerLevels[topicId];
        var npcTopicLevel = _npcLevels[topicId];
        if (playerTopicLevel < npcTopicLevel)
            num = num * LevelPointsReduce[npcTopicLevel - playerTopicLevel] / 100;
        return num;
    }

    private void _GiveExp(State before, State after)
    {
        // 完成论题的奖励
        if (before.Topics.Count > after.Topics.Count)
        {
            var finished = before.Topics.Where(t => after.Topics.All(at => at.ID != t.ID)).ToList();
            var total = 0;
            foreach (var topic in finished)
            {
                total += topic.Goals.Sum(goal => GoalPoints[goal - 1]);
                Points += _CalculateExp(topic.ID, total);
            }
        }

        // 合成卡牌的奖励
        var merged = before.Table.Where(c => after.Table.All(at => at != c)).ToList();
        foreach (var card in merged)
            Points += _CalculateExp(card.TopicID, Rewards[card.Value]);
    }
}