﻿using System;
using System.Collections.Generic;
using System.Text;
using Global;
using Global.Enums;
using Model.Solver.Helpers.Changes;
using Model.Solver.Position;
using Model.Solver.StrategiesUtility;

namespace Model.Solver.Strategies;

public class FishStrategy : AbstractStrategy
{
    public const string OfficialName = "Fish";
    private const OnCommitBehavior DefaultBehavior = OnCommitBehavior.Return;
    private const int MinUnitCount = 0;

    public override OnCommitBehavior DefaultOnCommitBehavior => DefaultBehavior;

    private int _minUnitCount;
    private int _maxUnitCount;
    private int _maxNumberOfExoFins;
    private int _maxNumberOfEndoFins;
    private bool _allowCannibalism;

    private static readonly CoverHouse[] CoverHouses =
    {
        new(Unit.Row, 0),
        new(Unit.Row, 1),
        new(Unit.Row, 2),
        new(Unit.Row, 3),
        new(Unit.Row, 4),
        new(Unit.Row, 5),
        new(Unit.Row, 6),
        new(Unit.Row, 7),
        new(Unit.Row, 8),
        new(Unit.Column, 0),
        new(Unit.Column, 1),
        new(Unit.Column, 2),
        new(Unit.Column, 3),
        new(Unit.Column, 4),
        new(Unit.Column, 5),
        new(Unit.Column, 6),
        new(Unit.Column, 7),
        new(Unit.Column, 8),
        new(Unit.MiniGrid, 0),
        new(Unit.MiniGrid, 1),
        new(Unit.MiniGrid, 2),
        new(Unit.MiniGrid, 3),
        new(Unit.MiniGrid, 4),
        new(Unit.MiniGrid, 5),
        new(Unit.MiniGrid, 6),
        new(Unit.MiniGrid, 7),
        new(Unit.MiniGrid, 8),
    };
    
    public FishStrategy(int minUnitCount, int maxUnitCount, int maxNumberOfExoFins, int maxNumberOfEndoFins,
        bool allowCannibalism) : base(OfficialName, StrategyDifficulty.Extreme, DefaultBehavior)
    {
        _minUnitCount = minUnitCount;
        _maxUnitCount = maxUnitCount;
        _maxNumberOfExoFins = maxNumberOfExoFins;
        _maxNumberOfEndoFins = maxNumberOfEndoFins;
        _allowCannibalism = allowCannibalism;
        ArgumentsList.Add(new MinMaxIntStrategyArgument("Unit count", 2, 4, 2, 5, 1,
            () => _minUnitCount, i => _minUnitCount = i, () => _maxUnitCount,
            i => _maxUnitCount = i));
        ArgumentsList.Add(new IntStrategyArgument("Max number of exo fins", () => _maxNumberOfExoFins,
            i => _maxNumberOfExoFins = i, new SliderViewInterface(0, 5, 1)));
        ArgumentsList.Add(new IntStrategyArgument("Max number of endo fins", () => _maxNumberOfEndoFins,
            i => _maxNumberOfEndoFins = i, new SliderViewInterface(0, 5, 1)));
        ArgumentsList.Add(new BooleanStrategyArgument("Allow cannibalism", () => _allowCannibalism,
            b => _allowCannibalism = b));
    }
    
    public override void Apply(IStrategyManager strategyManager)
    {
        for (int number = 1; number <= 9; number++)
        {
            var positions = strategyManager.PositionsFor(number);
            List<int> possibleCoverHouses = new();
            
            for (int i = 0; i < CoverHouses.Length; i++)
            {
                var current = CoverHouses[i];
                if (UnitMethods.Get(current.Unit).Count(positions, current.Number) > MinUnitCount) possibleCoverHouses.Add(i);
            }
            
            for (int unitCount = _minUnitCount; unitCount <= _maxUnitCount; unitCount++)
            {
                foreach (var combination in CombinationCalculator.EveryCombinationWithSpecificCount(unitCount, possibleCoverHouses))
                {
                    if (TryFind(strategyManager, number, combination)) return;
                }
            }
        }
    }

    private readonly GridPositions _toCover = new();
    private readonly HashSet<CoverHouse> _baseSet = new();
    private readonly GridPositions _buffer = new();
    private readonly HashSet<Cell> _endoFins = new();
    
    private bool TryFind(IStrategyManager strategyManager, int number, int[] combination)
    {
        _toCover.Void();
        _baseSet.Clear();
        _endoFins.Clear();
        var positions = strategyManager.PositionsFor(number);
        
        foreach (var n in combination)
        {
            var house = CoverHouses[n];
            var methods = UnitMethods.Get(house.Unit);

            if (methods.Count(_toCover, house.Number) > 0)
            {
                if (_maxNumberOfEndoFins == 0) return false;

                foreach (var c in methods.EveryCell(_toCover, house.Number))
                {
                    _endoFins.Add(c);
                    if (_endoFins.Count > _maxNumberOfEndoFins) return false;
                }
            }
            
            methods.Fill(_buffer, house.Number);
            _buffer.ApplyAnd(positions);
            _toCover.ApplyOr(_buffer);
            
            _baseSet.Add(house);
            _buffer.Void();
        }

        if (_maxNumberOfExoFins == 0)
        {
            foreach (var coverSet in _toCover.PossibleCoverHouses(combination.Length, _baseSet, UnitMethods.All))
            {
                if (Process(strategyManager, number, coverSet, _buffer)) return true;
            }
        }
        else
        {
            foreach (var coveredGrid in _toCover.PossibleCoveredGrids(combination.Length, 3, _baseSet,
                         UnitMethods.All))
            {
                if (Process(strategyManager, number, coveredGrid.CoverHouses, coveredGrid.Remaining)) return true;
            }
        }
        
        return false;
    }

    private readonly List<Cell> _fins = new();

    private bool Process(IStrategyManager strategyManager, int number, CoverHouse[] coverSet, IReadOnlyGridPositions exoFins)
    {
        _fins.Clear();
        var gpOfCoverSet = new GridPositions();
        
        foreach (var set in coverSet)
        {
            UnitMethods.Get(set.Unit).Fill(gpOfCoverSet, set.Number);
        }
        var diff = gpOfCoverSet.Difference(_toCover);

        _fins.AddRange(exoFins);
        _fins.AddRange(_endoFins);
        
        if (_fins.Count == 0)
        {
            foreach (var cell in diff)
            {
                strategyManager.ChangeBuffer.ProposePossibilityRemoval(number, cell);
            } 
        }
        else
        {
            foreach (var ssc in Cells.SharedSeenCells(_fins))
            {
                if (!diff.Peek(ssc)) continue;
                    
                strategyManager.ChangeBuffer.ProposePossibilityRemoval(number, ssc);
            }
        }

        if (_allowCannibalism) ProcessCannibalism(strategyManager, number, coverSet);
        
        return strategyManager.ChangeBuffer.NotEmpty() && strategyManager.ChangeBuffer.Commit(this,
                new FishReportBuilder(new HashSet<CoverHouse>(_baseSet), coverSet, number,
                    _toCover.Copy(), new List<Cell>(_fins))) && OnCommitBehavior == OnCommitBehavior.Return;
    }

    private void ProcessCannibalism(IStrategyManager strategyManager, int number, CoverHouse[] coverSet)
    {
        foreach (var cell in _toCover)
        {
            var count = 0;
            foreach (var house in coverSet)
            {
                if (UnitMethods.Get(house.Unit).Contains(cell, house.Number))
                {
                    count++;
                    if (count >= 2) break;
                }
            }

            if (count < 2) return;
            
            bool ok = true;
            foreach (var fin in _fins)
            {
                if (!Cells.ShareAUnit(fin, cell))
                {
                    ok = false;
                    break;
                }
            }
            
            if(ok) strategyManager.ChangeBuffer.ProposePossibilityRemoval(number, cell);
        }
    }
}

public class FishReportBuilder : IChangeReportBuilder
{
    private readonly HashSet<CoverHouse> _baseSet;
    private readonly CoverHouse[] _coveredSet;
    private readonly int _possibility;
    private readonly GridPositions _inCommon;
    private readonly List<Cell> _fins;

    public FishReportBuilder(HashSet<CoverHouse> baseSet, CoverHouse[] coveredSet, int possibility,
        GridPositions inCommon, List<Cell> fins)
    {
        _baseSet = baseSet;
        _coveredSet = coveredSet;
        _possibility = possibility;
        _inCommon = inCommon;
        _fins = fins;
    }

    public ChangeReport Build(List<SolverChange> changes, IPossibilitiesHolder snapshot)
    {
        return new ChangeReport(IChangeReportBuilder.ChangesToString(changes), Explanation(), lighter =>
        {
            foreach (var cell in _inCommon)
            {
                lighter.HighlightPossibility(_possibility, cell.Row, cell.Column, ChangeColoration.CauseOffOne);
            }

            foreach (var cell in _fins)
            {
                lighter.HighlightPossibility(_possibility, cell.Row, cell.Column, ChangeColoration.CauseOffTwo);
            }

            IChangeReportBuilder.HighlightChanges(lighter, changes);
        });
    }

    private string Explanation()
    {
        var type = _baseSet.Count switch
        {
            3 => "Swordfish",
            4 => "Jellyfish",
            _ => throw new ArgumentOutOfRangeException()
        };

        var baseSetBuilder = new StringBuilder();
        foreach (var ch in _baseSet)
        {
            baseSetBuilder.Append(ch + ", ");
        }

        var coverSetBuilder = new StringBuilder(_coveredSet[0].ToString());
        for (int i = 1; i < _coveredSet.Length; i++)
        {
            coverSetBuilder.Append(", " + _coveredSet[i]);
        }

        string isFinned = _fins.Count > 0 ? "Finned " : "";
        string fins = "Fins : ";
        if (_fins.Count > 0)
        {
            var finsBuilder = new StringBuilder(_fins[0].ToString());
            for (int i = 1; i < _fins.Count; i++)
            {
                finsBuilder.Append(", " + _fins[i]);
            }

            fins += finsBuilder.ToString();
        }

        return $"{isFinned}{type} found :\nBase set : {baseSetBuilder.ToString()[..^2]}\nCover set : {coverSetBuilder}" +
               $"\n{fins}";
    }
}