using System;
using System.Collections.Generic;
using System.Linq;
using AutoLunDao.Core.Entities;
using AutoLunDao.Core.Simulators;
using AutoLunDao.Core.Strategies;

namespace AutoLunDao.Benchmarks.Sandboxes;

using Card = Card;

/// <summary>
///     模拟「更好的论道Plus」Mod 论道规则的沙盒。
/// </summary>
public class BetterLunDaoPlusSandbox : ISandbox
{
    private const int MaxPlayerLevel = 5;
    private const int MaxNpcLevel = 5;

    private readonly List<Topic> _initialTopics;
    private readonly List<Card> _npcDeck;
    private readonly List<Card> _playerDeck;
    private readonly Random _random;

    private readonly int _seed;

    private readonly ISimulator _simulator;
    private int _randomCallCount; // 用于跟踪随机数调用次数，确保克隆时状态一致
    private State _state;

    public BetterLunDaoPlusSandbox(int seed, ISimulator simulator)
    {
        _seed = seed;
        _random = new Random(seed);

        _initialTopics = [];
        _playerDeck = [];
        _npcDeck = [];

        _simulator = simulator;

        _InitializeRandomData();
    }

    private BetterLunDaoPlusSandbox(BetterLunDaoPlusSandbox original)
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
    }

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
            decision = strategy.Decide(_state, _simulator);
            _state = _simulator.ApplyPlay(_state, decision);
        } while (decision is not null);

        return true;
    }

    public object Clone()
    {
        return new BetterLunDaoPlusSandbox(this);
    }

    private void _InitializeRandomData()
    {
        var topicsCount = _random.Next(1, 6);
        _randomCallCount += 1;

        for (var i = 0; i < topicsCount; i++)
        {
            // 为玩家和 NPC 随机生成悟道等级（悟道等级决定论道目标点数和手牌最大点数）
            var playerTopicLevel = _random.Next(1, MaxPlayerLevel + 1);
            var npcTopicLevel = _random.Next(1, MaxNpcLevel + 1);
            _randomCallCount += 2;

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

        _state = new State(_initialTopics.ToList(), [], [], topicsCount * 2 + 1, 0);
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
        for (var i = 0; i < 5; i++)
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
            decision = npc.Decide(state, _simulator);
            state = _simulator.ApplyPlay(state, decision);
        } while (decision is not null);

        _npcDeck.AddRange(state.Hand);

        return state;
    }
}