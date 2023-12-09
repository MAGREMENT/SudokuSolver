﻿using System.Collections.Generic;
using Model.Solver.Helpers.Changes;
using Model.Solver.Helpers.Highlighting;
using Model.Solver.Position;
using Model.Solver.Possibility;
using Model.Solver.StrategiesUtility;
using Model.Solver.StrategiesUtility.CellColoring;
using Model.Solver.StrategiesUtility.CellColoring.ColoringResults;
using Model.Solver.StrategiesUtility.Graphs;

namespace Model.Solver.Strategies.ForcingNets;

public class NishioForcingNetStrategy : AbstractStrategy
{ 
    public const string OfficialName = "Nishio Forcing Net";
    private const OnCommitBehavior DefaultBehavior = OnCommitBehavior.Return;
    
    public override OnCommitBehavior DefaultOnCommitBehavior => DefaultBehavior;

    public NishioForcingNetStrategy() : base(OfficialName, StrategyDifficulty.Extreme, DefaultBehavior)
    { }

    public override void Apply(IStrategyManager strategyManager)
    {
        ContradictionSearcher cs = new ContradictionSearcher(strategyManager);

        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                foreach (var possibility in strategyManager.PossibilitiesAt(row, col))
                {
                    var coloring = strategyManager.PreComputer.OnColoring(row, col, possibility);
                    foreach (var entry in coloring)
                    {
                        if (entry.Key is not CellPossibility cell) continue;
                        
                        switch (entry.Value)
                        {
                            case Coloring.Off when cs.AddOff(cell):
                                strategyManager.ChangeBuffer.ProposePossibilityRemoval(possibility, row, col);
                                
                                if (strategyManager.ChangeBuffer.NotEmpty() && strategyManager.ChangeBuffer
                                        .Commit(this, new NishioForcingNetReportBuilder(coloring, row, col, 
                                            possibility, cs.Cause, cell, Coloring.Off, strategyManager.GraphManager.ComplexLinkGraph)) && 
                                                OnCommitBehavior == OnCommitBehavior.Return) return;
                                break;
                            
                            case Coloring.On when cs.AddOn(cell):
                                strategyManager.ChangeBuffer.ProposePossibilityRemoval(possibility, row, col);
                                
                                if (strategyManager.ChangeBuffer.NotEmpty() && strategyManager.ChangeBuffer
                                        .Commit(this, new NishioForcingNetReportBuilder(coloring, row, col,
                                            possibility, cs.Cause, cell, Coloring.On, strategyManager.GraphManager.ComplexLinkGraph)) && 
                                                OnCommitBehavior == OnCommitBehavior.Return) return;
                                break;
                        }
                    }

                    cs.Reset();
                }
            }
        }
    }
}

public class ContradictionSearcher
{
    private readonly Dictionary<int, Possibilities> _offCells = new();
    private readonly Dictionary<int, LinePositions> _offRows = new();
    private readonly Dictionary<int, LinePositions> _offCols = new();
    private readonly Dictionary<int, MiniGridPositions> _offMinis = new();

    private readonly Dictionary<int, GridPositions> _onPositions = new();

    private readonly IStrategyManager _view;

    public ContradictionCause Cause { get; private set; } = ContradictionCause.None;

    public ContradictionSearcher(IStrategyManager view)
    {
        _view = view;
    }

    /// <summary>
    /// Returns true if a contradiction if found
    /// </summary>
    /// <param name="cell"></param>
    /// <returns></returns>
    public bool AddOff(CellPossibility cell)
    {
        var cellInt = cell.Row * 9 + cell.Column;
        if (!_offCells.TryGetValue(cellInt, out var poss))
        {
            var copy = _view.PossibilitiesAt(cell.Row, cell.Column).Copy();
            copy.Remove(cell.Possibility);
            _offCells[cellInt] = copy;
        }
        else
        {
            poss.Remove(cell.Possibility);
            if (poss.Count == 0)
            {
                Cause = ContradictionCause.Cell;
                return true;
            }
        }

        var rowInt = cell.Row * 9 + cell.Possibility;
        if (!_offRows.TryGetValue(rowInt, out var rowPos))
        {
            var copy = _view.RowPositionsAt(cell.Row, cell.Possibility).Copy();
            _offRows[rowInt] = copy;
            rowPos = copy;
        }
        
        rowPos.Remove(cell.Column);
        if (rowPos.Count == 0)
        {
            Cause = ContradictionCause.Row;
            return true;
        }
        

        var colInt = cell.Column * 9 + cell.Possibility;
        if (!_offCols.TryGetValue(colInt, out var colPos))
        {
            var copy = _view.ColumnPositionsAt(cell.Column, cell.Possibility).Copy();
            _offCols[colInt] = copy;
            colPos = copy;
        }
        
        colPos.Remove(cell.Row);
        if (colPos.Count == 0)
        {
            Cause = ContradictionCause.Column;
            return true;
        }
        

        var miniInt = cell.Row / 3 + cell.Column / 3 * 3 + cell.Possibility * 9;
        if (!_offMinis.TryGetValue(miniInt, out var miniPos))
        {
            var copy = _view.MiniGridPositionsAt(cell.Row / 3,
                cell.Column / 3, cell.Possibility).Copy();
            _offMinis[miniInt] = copy;
            miniPos = copy;
        }
        
        miniPos.Remove(cell.Row % 3, cell.Column % 3);
        if (miniPos.Count == 0)
        {
            Cause = ContradictionCause.MiniGrid;
            return true;
        }
        

        return false;
    }

    public bool AddOn(CellPossibility cell)
    {
        _onPositions.TryAdd(cell.Possibility, new GridPositions());
        var possibilityPositions = _onPositions[cell.Possibility];
        possibilityPositions.Add(cell.Row, cell.Column);

        if (possibilityPositions.RowCount(cell.Row) > 1)
        {
            Cause = ContradictionCause.Row;
            return true;
        }

        if (possibilityPositions.ColumnCount(cell.Column) > 1)
        {
            Cause = ContradictionCause.Column;
            return true;
        }

        if (possibilityPositions.MiniGridCount(cell.Row / 3, cell.Column / 3) > 1)
        {
            Cause = ContradictionCause.MiniGrid;
            return true;
        }
        
        foreach (var entry in _onPositions)
        {
            if (entry.Key != cell.Possibility && entry.Value.Peek(cell.Row, cell.Column))
            {
                Cause = ContradictionCause.Cell;
                return true;
            }
        }

        return false;
    }

    public void Reset()
    {
        _offCells.Clear();
        _offRows.Clear();
        _offCols.Clear();
        _offMinis.Clear();

        _onPositions.Clear();

        Cause = ContradictionCause.None;
    }
}

public enum ContradictionCause
{
    None, Row, Column, MiniGrid, Cell
}

public class NishioForcingNetReportBuilder : IChangeReportBuilder
{
    private readonly ColoringDictionary<ILinkGraphElement> _coloring;
    private readonly int _row;
    private readonly int _col;
    private readonly int _possibility;
    private readonly ContradictionCause _cause;
    private readonly CellPossibility _lastChecked;
    private readonly Coloring _causeColoring;
    private readonly LinkGraph<ILinkGraphElement> _graph;

    public NishioForcingNetReportBuilder(ColoringDictionary<ILinkGraphElement> coloring, int row, int col,
        int possibility, ContradictionCause cause, CellPossibility lastChecked, Coloring causeColoring, LinkGraph<ILinkGraphElement> graph)
    {
        _coloring = coloring;
        _row = row;
        _col = col;
        _possibility = possibility;
        _cause = cause;
        _lastChecked = lastChecked;
        _causeColoring = causeColoring;
        _graph = graph;
    }

    public ChangeReport Build(List<SolverChange> changes, IPossibilitiesHolder snapshot)
    {
        List<Highlight> highlighters = new();
        switch (_cause)
        {
            case ContradictionCause.Cell :
                var possibilities = snapshot.PossibilitiesAt(_lastChecked.Row, _lastChecked.Column);

                foreach (var possibility in possibilities)
                {
                    var current = new CellPossibility(_lastChecked.Row, _lastChecked.Column, possibility);
                    if (!_coloring.TryGetColoredElement(current, out var c) || c != _causeColoring) continue;
                        
                    highlighters.Add( lighter =>
                    {
                        var paths = ForcingNetsUtility.FindEveryNeededPaths(_coloring.History!
                                .GetPathToRootWithGuessedLinks(current, c), _coloring, _graph, snapshot);
                        ForcingNetsUtility.HighlightAllPaths(lighter, paths, Coloring.Off);
                        
                        lighter.EncirclePossibility(_possibility, _row, _col);
                        IChangeReportBuilder.HighlightChanges(lighter, changes);
                    });
                }
                break;
            case ContradictionCause.Row :
                var cols = snapshot.RowPositionsAt(_lastChecked.Row, _lastChecked.Possibility);
                
                foreach (var col in cols)
                {
                    var current = new CellPossibility(_lastChecked.Row, col, _lastChecked.Possibility);
                    if (!_coloring.TryGetColoredElement(current, out var c) || c != _causeColoring) continue;
                    
                    highlighters.Add(lighter =>
                    {
                        var paths = ForcingNetsUtility.FindEveryNeededPaths(_coloring.History!
                            .GetPathToRootWithGuessedLinks(current, c), _coloring, _graph, snapshot);
                        ForcingNetsUtility.HighlightAllPaths(lighter, paths, Coloring.Off);
                        
                        lighter.EncirclePossibility(_possibility, _row, _col);
                        IChangeReportBuilder.HighlightChanges(lighter, changes);
                    });
                }
                
                break;
            case ContradictionCause.Column :
                var rows = snapshot.ColumnPositionsAt(_lastChecked.Column, _lastChecked.Possibility);
                
                foreach (var row in rows)
                {
                    var current = new CellPossibility(row, _lastChecked.Column, _lastChecked.Possibility);
                    if (!_coloring.TryGetColoredElement(current, out var c) || c != _causeColoring) continue;
                    
                    highlighters.Add(lighter =>
                    {
                        var paths = ForcingNetsUtility.FindEveryNeededPaths(_coloring.History!
                            .GetPathToRootWithGuessedLinks(current, c), _coloring, _graph, snapshot);
                        ForcingNetsUtility.HighlightAllPaths(lighter, paths, Coloring.Off);
                        
                        lighter.EncirclePossibility(_possibility, _row, _col);
                        IChangeReportBuilder.HighlightChanges(lighter, changes);
                    });
                }
                
                break;
            case ContradictionCause.MiniGrid :
                var cells = snapshot.MiniGridPositionsAt(_lastChecked.Row / 3,
                    _lastChecked.Column / 3, _lastChecked.Possibility);
              
                foreach (var cell in cells)
                {
                    var current = new CellPossibility(cell.Row, cell.Column, _lastChecked.Possibility);
                    if (!_coloring.TryGetColoredElement(current, out var c) || c != _causeColoring) continue;
                    
                    highlighters.Add(lighter =>
                    {
                        var paths = ForcingNetsUtility.FindEveryNeededPaths(_coloring.History!
                            .GetPathToRootWithGuessedLinks(current, c), _coloring, _graph, snapshot);
                        ForcingNetsUtility.HighlightAllPaths(lighter, paths, Coloring.Off);
                        
                        lighter.EncirclePossibility(_possibility, _row, _col);
                        IChangeReportBuilder.HighlightChanges(lighter, changes);
                    });
                }
                
                break;
        }

        return new ChangeReport(IChangeReportBuilder.ChangesToString(changes), Explanation(), highlighters.ToArray());
    }

    private string Explanation()
    {
        var result = $"{_possibility}r{_row + 1}c{_col + 1} being ON will lead to ";

        result += _causeColoring == Coloring.On ? "multiple candidates being ON in " : "all candidates being OFF in ";

        result += _cause switch
        {
            ContradictionCause.Cell => $"r{_lastChecked.Row + 1}c{_lastChecked.Column + 1}",
            ContradictionCause.Row => $"n{_lastChecked.Possibility}r{_lastChecked.Row + 1}",
            ContradictionCause.Column => $"n{_lastChecked.Possibility}c{_lastChecked.Column + 1}",
            ContradictionCause.MiniGrid => $"n{_lastChecked.Possibility}b{_lastChecked.Row * 3 + _lastChecked.Column + 1}",
            _ => ""
        };

        return result;
    }
}