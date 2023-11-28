﻿using System.Collections.Generic;
using Model.Solver.Strategies;
using Model.Solver.Strategies.AlternatingChains;
using Model.Solver.Strategies.AlternatingChains.ChainAlgorithms;
using Model.Solver.Strategies.AlternatingChains.ChainTypes;
using Model.Solver.Strategies.FishGeneralization;
using Model.Solver.Strategies.FishGeneralization.FishTypes;
using Model.Solver.Strategies.ForcingNets;
using Model.Solver.Strategies.NRCZTChains;
using Model.Solver.Strategies.SetEquivalence;
using Model.Solver.Strategies.SetEquivalence.Searchers;
using Model.Solver.StrategiesUtility;
using Model.Solver.StrategiesUtility.Graphs;
using Model.Utility;

namespace Model.Solver.Helpers;

public class StrategyLoader : IStrategyLoader //TODO => Handle isUsed on false and strategy shuffle
{
    private static readonly Dictionary<string, IStrategy> StrategyPool = new()
    {
        {NakedSingleStrategy.OfficialName, new NakedSingleStrategy()},
        {HiddenSingleStrategy.OfficialName, new HiddenSingleStrategy()},
        {NakedDoublesStrategy.OfficialName, new NakedDoublesStrategy()},
        {HiddenDoublesStrategy.OfficialName, new HiddenDoublesStrategy()},
        {BoxLineReductionStrategy.OfficialName, new BoxLineReductionStrategy()},
        {PointingSetStrategy.OfficialName, new PointingSetStrategy()},
        {NakedSetStrategy.OfficialNameForType3, new NakedSetStrategy(3)},
        {HiddenSetStrategy.OfficialNameForType3, new HiddenSetStrategy(3)},
        {NakedSetStrategy.OfficialNameForType4, new NakedSetStrategy(4)},
        {HiddenSetStrategy.OfficialNameForType4, new HiddenSetStrategy(4)},
        {XWingStrategy.OfficialName, new XWingStrategy()},
        {XYWingStrategy.OfficialName, new XYWingStrategy()},
        {XYZWingStrategy.OfficialName, new XYZWingStrategy()},
        {GridFormationStrategy.OfficialNameForType3, new GridFormationStrategy(3)},
        {GridFormationStrategy.OfficialNameForType4, new GridFormationStrategy(4)},
        {SimpleColoringStrategy.OfficialName, new SimpleColoringStrategy()},
        {BUGStrategy.OfficialName, new BUGStrategy()},
        {ReverseBUGStrategy.OfficialName, new ReverseBUGStrategy()},
        {JuniorExocetStrategy.OfficialName, new JuniorExocetStrategy()},
        {FinnedXWingStrategy.OfficialName, new FinnedXWingStrategy()},
        {FinnedGridFormationStrategy.OfficialNameForType3, new FinnedGridFormationStrategy(3)},
        {FinnedGridFormationStrategy.OfficialNameForType4, new FinnedGridFormationStrategy(4)},
        {FireworksStrategy.OfficialName, new FireworksStrategy()},
        {UniqueRectanglesStrategy.OfficialName, new UniqueRectanglesStrategy()},
        {UnavoidableRectanglesStrategy.OfficialName, new UnavoidableRectanglesStrategy()},
        {XYChainsStrategy.OfficialName, new XYChainsStrategy()},
        {ThreeDimensionMedusaStrategy.OfficialName, new ThreeDimensionMedusaStrategy()},
        {WXYZWingStrategy.OfficialName, new WXYZWingStrategy()},
        {AlignedPairExclusionStrategy.OfficialName, new AlignedPairExclusionStrategy()},
        {SubsetsXCycles.OfficialName, new AlternatingChainGeneralization<ILinkGraphElement>(new SubsetsXCycles(),
            new AlternatingChainAlgorithmV3<ILinkGraphElement>())},
        {SueDeCoqStrategy.OfficialName, new SueDeCoqStrategy()},
        {AlmostLockedSetsStrategy.OfficialName, new AlmostLockedSetsStrategy()},
        {SubsetsAlternatingInferenceChains.OfficialName, new AlternatingChainGeneralization<ILinkGraphElement>(new SubsetsAlternatingInferenceChains(),
            new AlternatingChainAlgorithmV3<ILinkGraphElement>())},
        {DigitForcingNetStrategy.OfficialName, new DigitForcingNetStrategy()},
        {CellForcingNetStrategy.OfficialName, new CellForcingNetStrategy(4)},
        {UnitForcingNetStrategy.OfficialName, new UnitForcingNetStrategy(4)},
        {NishioForcingNetStrategy.OfficialName, new NishioForcingNetStrategy()},
        {PatternOverlayStrategy.OfficialName, new PatternOverlayStrategy(1, 1000)},
        {BruteForceStrategy.OfficialName, new BruteForceStrategy()},
        {SKLoopsStrategy.OfficialName, new SKLoopsStrategy()},
        {GurthTheorem.OfficialName, new GurthTheorem()},
        {SetEquivalenceStrategy.OfficialName, new SetEquivalenceStrategy(new RowsAndColumnsSearcher(2,
            5, 5), new PhistomefelRingLikeSearcher())},
        {DeathBlossomStrategy.OfficialName, new DeathBlossomStrategy()},
        {AlmostHiddenSetsStrategy.OfficialName, new AlmostHiddenSetsStrategy()},
        {AlignedTripleExclusionStrategy.OfficialName, new AlignedTripleExclusionStrategy(5)},
        {BUGLiteStrategy.OfficialName, new BUGLiteStrategy()},
        {EmptyRectangleStrategy.OfficialName, new EmptyRectangleStrategy()},
        {NRCZTChainStrategy.OfficialNameForDefault, new NRCZTChainStrategy()},
        {NRCZTChainStrategy.OfficialNameForTCondition, new NRCZTChainStrategy(new TCondition())},
        {NRCZTChainStrategy.OfficialNameForZCondition, new NRCZTChainStrategy(new ZCondition())},
        {NRCZTChainStrategy.OfficialNameForZAndTCondition, new NRCZTChainStrategy(new TCondition(), new ZCondition())},
        {AlternatingInferenceChains.OfficialName, new AlternatingChainGeneralization<CellPossibility>(new AlternatingInferenceChains(),
            new AlternatingChainAlgorithmV3<CellPossibility>())},
        {XCycles.OfficialName, new AlternatingChainGeneralization<CellPossibility>(new XCycles(),
            new AlternatingChainAlgorithmV3<CellPossibility>())},
        {SkyscraperStrategy.OfficialName, new SkyscraperStrategy()},
        {FishGeneralization.OfficialNameForBasic, new FishGeneralization(3, 4, new BasicFish())},
        {TwoStringKiteStrategy.OfficialName, new TwoStringKiteStrategy()},
        {AlmostLockedSetsChainStrategy.OfficialName, new AlmostLockedSetsChainStrategy(false)}
    };

    private readonly UniqueList<IStrategy> _strategies = new();
    private readonly BitSet _excludedStrategies = new();
    private readonly BitSet _lockedStrategies = new();
    
    public IReadOnlyList<IStrategy> Strategies => _strategies;
    private readonly IStrategyRepository _repository;

    public event OnListUpdate? ListUpdated;
    private bool _callEvent = true;

    public StrategyLoader(IStrategyRepository repository)
    {
        _repository = repository;
        ListUpdated += UpdateRepository;
    }

    public void Load()
    {
        _callEvent = false;
        
        _strategies.Clear();
        _excludedStrategies.Clear();
        _lockedStrategies.Clear();

        var download = _repository.DownloadStrategies();
        var count = 0;
        foreach (var dao in download)
        {
            if (!StrategyPool.TryGetValue(dao.Name, out var strategy)) continue;
            
            strategy.OnCommitBehavior = dao.Behavior;
            _strategies.Add(StrategyPool[dao.Name], _ => {});
            if (!dao.Used) ExcludeStrategy(count);
            count++;
        }

        _callEvent = true;
        TryCallEvent();
    }
    
    public void ExcludeStrategy(int number)
    {
        if (IsStrategyLocked(number)) return;

        _excludedStrategies.Set(number);
        TryCallEvent();
    }
    
    public void UseStrategy(int number)
    {
        if (IsStrategyLocked(number)) return;

        _excludedStrategies.Unset(number);
        TryCallEvent();
    }

    public bool IsStrategyUsed(int number)
    {
        return !_excludedStrategies.IsSet(number);
    }

    public bool IsStrategyLocked(int number)
    {
        return _lockedStrategies.IsSet(number);
    }
    
    public void AllowUniqueness(bool yes)
    {
        for (int i = 0; i < Strategies.Count; i++)
        {
            if (Strategies[i].UniquenessDependency != UniquenessDependency.FullyDependent) continue;
            
            if (yes) UnLockStrategy(i);
            else
            {
                ExcludeStrategy(i);
                LockStrategy(i);
            }
        }
    }
    
    public StrategyInformation[] GetStrategiesInformation()
    {
        StrategyInformation[] result = new StrategyInformation[Strategies.Count];

        for (int i = 0; i < Strategies.Count; i++)
        {
            result[i] = new StrategyInformation(Strategies[i], IsStrategyUsed(i), IsStrategyLocked(i));
        }

        return result;
    }

    public List<string> FindStrategies(string filter)
    {
        List<string> result = new();
        var lFilter = filter.ToLower();

        foreach (var name in StrategyPool.Keys)
        {
            if (name.ToLower().Contains(lFilter)) result.Add(name);
        }

        return result;
    }
    
    public void AddStrategy(string name)
    {
        _strategies.Add(StrategyPool[name], i => InterchangeStrategies(i, _strategies.Count));
        TryCallEvent();
    }

    public void AddStrategy(string name, int position)
    {
        if (position == _strategies.Count)
        {
            AddStrategy(name);
            return;
        }

        _strategies.InsertAt(StrategyPool[name], position, i => InterchangeStrategies(i, position));
        InsertInBitSets(position);
        TryCallEvent();
    }
    
    public void RemoveStrategy(int position)
    {
        _strategies.RemoveAt(position);
        DeleteInBitSets(position);
        TryCallEvent();
    }
    
    public void InterchangeStrategies(int positionOne, int positionTwo)
    {
        var diff = positionTwo - positionOne;
        if (diff is 1 or 0) return;
        
        var buffer = _strategies[positionOne].Name;
        var wasUsed = IsStrategyUsed(positionOne);
        var wasLocked = IsStrategyLocked(positionOne);
        
        _strategies.RemoveAt(positionOne);
        DeleteInBitSets(positionOne);
        var newPosTwo = positionTwo > positionOne ? positionTwo - 1 : positionTwo;
        
        _callEvent = false;
        
        AddStrategy(buffer, newPosTwo);
        if (!wasUsed) ExcludeStrategy(newPosTwo);
        if (wasLocked) LockStrategy(newPosTwo);
        
        _callEvent = true;
        TryCallEvent();
    }

    public void ChangeStrategyBehavior(string name, OnCommitBehavior behavior)
    {
        var i = _strategies.Find(s => s.Name.Equals(name));
        if (i != 0) _strategies[i].OnCommitBehavior = behavior;
    }
    
    public void ChangeStrategyUsage(string name, bool yes)
    {
        var i = _strategies.Find(s => s.Name.Equals(name));
        if (i != -1)
        {
            if (yes) _excludedStrategies.Unset(i);
            else _excludedStrategies.Set(i);
        }
    }

    //Private-----------------------------------------------------------------------------------------------------------
    
    private void LockStrategy(int n)
    {
        _lockedStrategies.Set(n);
    }

    private void UnLockStrategy(int n)
    {
        _lockedStrategies.Unset(n);
    }

    private void DeleteInBitSets(int n)
    {
        _lockedStrategies.Delete(n);
        _excludedStrategies.Delete(n);
    }

    private void InsertInBitSets(int n)
    {
        _lockedStrategies.Insert(n);
        _excludedStrategies.Insert(n);
    }

    private void TryCallEvent()
    {
        if (!_callEvent) return;
        
        ListUpdated?.Invoke();
    }

    private void UpdateRepository()
    {
        var list = new List<StrategyDAO>(_strategies.Count);
        for (int i = 0; i < _strategies.Count; i++)
        {
            var s = _strategies[i];
            list.Add(new StrategyDAO(s.Name, IsStrategyUsed(i), s.OnCommitBehavior, new Dictionary<string, string>()));
        }

        _repository.UploadStrategies(list);
    }
}

public class StrategyInformation
{
    public string StrategyName { get; }
    public StrategyDifficulty Difficulty { get; }
    public bool Used { get; }
    public bool Locked { get; }
    public OnCommitBehavior Behavior { get; }
    public IReadOnlyTracker Tracker { get; }

    public StrategyInformation(IStrategy strategy, bool used, bool locked)
    {
        StrategyName = strategy.Name;
        Difficulty = strategy.Difficulty;
        Used = used;
        Locked = locked;
        Behavior = strategy.OnCommitBehavior;
        Tracker = strategy.Tracker;
    }
}

public delegate void OnListUpdate();