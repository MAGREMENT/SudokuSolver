﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using Model.Core;
using Model.Core.Changes;
using Model.Core.Highlighting;
using Model.Sudokus.Solver.Position;
using Model.Sudokus.Solver.Utility;
using Model.Utility.BitSets;

namespace Model.Sudokus.Solver.Strategies.MultiSector;

public class MultiSectorLockedSetsStrategy : SudokuStrategy
{
    public const string OfficialName = "Multi-Sector Locked Sets";
    private const InstanceHandling DefaultInstanceHandling = InstanceHandling.FirstOnly;

    private readonly IMultiSectorCellsSearcher[] _cellsSearchers;
    
    public MultiSectorLockedSetsStrategy(params IMultiSectorCellsSearcher[] searchers) : base(OfficialName,
        StepDifficulty.Extreme, DefaultInstanceHandling)
    {
        _cellsSearchers = searchers;
    }
    
    public override void Apply(ISudokuStrategyUser strategyUser)
    {
        List<PossibilityCovers> covers = new();
        foreach (var searcher in _cellsSearchers)
        {
            foreach (var grid in searcher.SearchGrids(strategyUser))
            {
                var count = 0;
                
                for (int number = 1; number <= 9; number++)
                {
                    var positions = strategyUser.PositionsFor(number);
                    var and = grid.And(positions);
                    if (and.Count == 0) continue;

                    var coverHouses = and.BestCoverHouses(UnitMethods.All);
                    count += coverHouses.Count;
                    covers.Add(new PossibilityCovers(number, coverHouses.ToArray()));
                }

                if (count == grid.Count && Process(strategyUser, grid, covers)) return;
                covers.Clear();
            }
        }
    }

    private bool Process(ISudokuStrategyUser strategyUser, GridPositions grid, List<PossibilityCovers> covers)
    {
        List<PossibilityCovers> alternativesTotal = new();
        HashSet<House> emptyForbidden = new();
        
        foreach (var cover in covers)
        {
            var positions = strategyUser.PositionsFor(cover.Possibility);
            var and = grid.And(positions);

            var alternatives = and.PossibleCoverHouses(cover.CoverHouses.Length, emptyForbidden, UnitMethods.All);
            foreach (var alternative in alternatives)
            {
                if (alternatives.Count > 1 && !IsSameCoverHouses(alternative, cover.CoverHouses))
                    alternativesTotal.Add(cover with { CoverHouses = alternative });
                
                foreach (var house in alternative)
                {
                    var method = UnitMethods.Get(house.Unit);
                    foreach (var cell in method.EveryCell(house.Number))
                    {
                        if (grid.Contains(cell)) continue;
                        strategyUser.ChangeBuffer.ProposePossibilityRemoval(cover.Possibility, cell);
                    }
                }
            } 
        }

        return strategyUser.ChangeBuffer.NotEmpty() && strategyUser.ChangeBuffer.Commit(
            new MultiSectorLockedSetsReportBuilder(grid, covers.ToArray(), alternativesTotal)) && StopOnFirstPush;
    }

    private bool IsSameCoverHouses(House[] one, House[] two)
    {
        if (one.Length != two.Length) return false;
        
        foreach (var house in one)
        {
            if (!two.Contains(house)) return false;
        }

        return true;
    }
}

public interface IMultiSectorCellsSearcher
{
    public IEnumerable<GridPositions> SearchGrids(ISudokuStrategyUser strategyUser);
}

public class MultiSectorLockedSetsReportBuilder : IChangeReportBuilder<IUpdatableSudokuSolvingState, ISudokuHighlighter>
{
    private readonly GridPositions _grid;
    private readonly IReadOnlyList<PossibilityCovers> _covers;
    private readonly IReadOnlyList<PossibilityCovers> _alternatives;

    public MultiSectorLockedSetsReportBuilder(GridPositions grid, IReadOnlyList<PossibilityCovers> covers, List<PossibilityCovers> alternatives)
    {
        _grid = grid;
        _covers = covers;
        _alternatives = alternatives;
    }

    public ChangeReport<ISudokuHighlighter> BuildReport(IReadOnlyList<SolverProgress> changes, IUpdatableSudokuSolvingState snapshot)
    {
        var cu = new CoveringUnits(_covers);
        
        return new ChangeReport<ISudokuHighlighter>( Explanation(cu), lighter =>
        {
            foreach (var cell in _grid)
            {
                lighter.HighlightCell(cell, ChangeColoration.Neutral);
            }

            ChangeReportHelper.HighlightChanges(lighter, changes);
        }, cu.Highlight(snapshot, _grid));
    }

    private string Explanation(CoveringUnits cu)
    {
        var builder = new StringBuilder("Cells : ");

        int count = 0;
        foreach (var cell in _grid)
        {
            builder.Append(count++ == _grid.Count - 1 ? cell.ToString() : cell + ", ");
        }

        builder.Append("\nLinks : " + cu);

        builder.Append("\nAlternatives : ");
        foreach (var alt in _alternatives)
        {
            builder.Append($"\n{alt.Possibility} : {alt.CoverHouses[0]}");
            for (int i = 1; i < alt.CoverHouses.Length; i++)
            {
                builder.Append($", {alt.CoverHouses[i]}");
            }
        }
        
        return builder.ToString();
    }
    
    public Clue<ISudokuHighlighter> BuildClue(IReadOnlyList<SolverProgress> changes, IUpdatableSudokuSolvingState snapshot)
    {
        return Clue<ISudokuHighlighter>.Default();
    }
}

public class CoveringUnits
{
    private readonly Dictionary<House, ReadOnlyBitSet16> _dictionary = new();

    public CoveringUnits(IReadOnlyList<PossibilityCovers> list)
    {
        foreach (var cover in list)
        {
            foreach (var house in cover.CoverHouses)
            {
                if (!_dictionary.ContainsKey(house))
                {
                    _dictionary[house] = new ReadOnlyBitSet16();
                }

                _dictionary[house] += cover.Possibility;
            }
        }
    }

    public Highlight<ISudokuHighlighter>[] Highlight(IUpdatableSudokuSolvingState snapshot, GridPositions gp)
    {
        var n = (int)ChangeColoration.CauseOffOne;
        var i = 0;
        List<Highlight<ISudokuHighlighter>> h = new();

        foreach (var entry in _dictionary)
        {
            var color = (ChangeColoration)(n + i);
            
            h.Add(lighter =>
            {
                foreach (var cell in gp)
                {
                    lighter.HighlightCell(cell, ChangeColoration.Neutral);
                }
                
                lighter.EncircleHouse(entry.Key, color);
                foreach (var cell in UnitMethods.Get(entry.Key.Unit).EveryCell(entry.Key.Number))
                {
                    if (!gp.Contains(cell)) continue;
                    
                    foreach (var p in entry.Value.EnumeratePossibilities())
                    {
                        if(snapshot.PossibilitiesAt(cell).Contains(p)) lighter.HighlightPossibility(p, cell.Row, cell.Column, color);
                    }
                }
            });
            
            i = (i + 1) % 10;
        }

        return h.ToArray();
    }

    public override string ToString()
    {
        var result = new StringBuilder();

        foreach (var entry in _dictionary)
        {
            foreach (var p in entry.Value.EnumeratePossibilities())
            {
                result.Append(p);
            }

            result.Append(entry.Key + ", ");
        }

        return result.ToString()[..^2];
    }
}