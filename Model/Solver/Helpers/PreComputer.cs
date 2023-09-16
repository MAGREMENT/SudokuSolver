﻿using System.Collections.Generic;
using Model.Solver.Possibilities;
using Model.Solver.StrategiesUtil;
using Model.Solver.StrategiesUtil.LinkGraph;
using Model.StrategiesUtil;
using Model.StrategiesUtil.LinkGraph;

namespace Model.Solver.Helpers;

public class PreComputer
{
    private readonly IStrategyManager _view;

    private List<AlmostLockedSet>? _als;

    private readonly LinkGraphManager _graphManager;
    private bool _graphConstructed;

    private readonly Dictionary<ILinkGraphElement, Coloring>?[,,] _onColoring
        = new Dictionary<ILinkGraphElement, Coloring>[9, 9, 9];
    private bool _wasPreColorUsed;

    public PreComputer(IStrategyManager view)
    {
        _view = view;
        _graphManager = new LinkGraphManager(_view);
    }

    public void Reset()
    {
        _als = null;
        
        _graphManager.Clear();
        _graphConstructed = false;

        if (_wasPreColorUsed)
        {
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    for (int k = 0; k < 9; k++)
                    {
                        _onColoring[i, j, k] = null;
                    }
                }
            }
            _wasPreColorUsed = false;
        }
    }

    public List<AlmostLockedSet> AlmostLockedSets()
    {
        _als ??= DoAlmostLockedSets();
        return _als;
    }

    public LinkGraph<ILinkGraphElement> LinkGraph()
    {
        if (!_graphConstructed)
        {
            _graphManager.Construct();
            _graphConstructed = true;
        }
        return _graphManager.LinkGraph;
    }

    public Dictionary<ILinkGraphElement, Coloring> OnColoring(int row, int col, int possibility)
    {
        _wasPreColorUsed = true;

        _onColoring[row, col, possibility - 1] ??=
            DoColor(new CellPossibility(row, col, possibility), Coloring.On);
        return _onColoring[row, col, possibility - 1]!;
    }
    
    public Dictionary<ILinkGraphElement, Coloring> OffColoring(int row, int col, int possibility)
    {
        return DoColor(new CellPossibility(row, col, possibility), Coloring.Off);
    }

    private List<AlmostLockedSet> DoAlmostLockedSets()
    {
        var result = new List<AlmostLockedSet>();

        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                if (_view.Sudoku[row, col] != 0) continue;

                if (_view.PossibilitiesAt(row, col).Count == 2)
                    result.Add(new AlmostLockedSet(new Cell(row, col), _view.PossibilitiesAt(row, col)));
                SearchRow(_view, row, col + 1, _view.PossibilitiesAt(row, col), 
                    new List<Cell> {new (row, col)}, result);
            }
        }
        
        for (int col = 0; col < 9; col++)
        {
            for (int row = 0; row < 9; row++)
            {
                if (_view.Sudoku[row, col] != 0) continue;
                
                SearchColumn(_view, col, row + 1, _view.PossibilitiesAt(row, col), 
                    new List<Cell> {new (row, col)}, result);
            }
        }

        for (int miniRow = 0; miniRow < 3; miniRow++)
        {
            for (int miniCol = 0; miniCol < 3; miniCol++)
            {
                for (int n = 0; n < 9; n++)
                {
                    int row = miniRow * 3 + n / 3;
                    int col = miniCol * 3 + n % 3;
                    if (_view.Sudoku[row, col] != 0) continue;
                    
                    SearchMiniGrid(_view, miniRow, miniCol, n + 1, _view.PossibilitiesAt(row, col),
                        new List<Cell> {new (row, col)}, result);
                }
            }
        }

        return result;
    }

    private void SearchRow(IStrategyManager strategyManager, int row, int start, IReadOnlyPossibilities current,
        List<Cell> visited, List<AlmostLockedSet> result)
    {
        for (int col = start; col < 9; col++)
        {
            if (strategyManager.Sudoku[row, col] != 0) continue;

            IPossibilities mashed = current.Or(strategyManager.PossibilitiesAt(row, col));
            if (mashed.Count == current.Count + strategyManager.PossibilitiesAt(row, col).Count) continue;

            var copy = new List<Cell>(visited) { new (row, col) };

            if (mashed.Count == copy.Count + 1)
            {
                result.Add(new AlmostLockedSet(copy.ToArray(), mashed));
            }

            SearchRow(strategyManager, row, col + 1, mashed, copy, result);
        }
    }

    private void SearchColumn(IStrategyManager strategyManager, int col, int start, IReadOnlyPossibilities current,
        List<Cell> visited, List<AlmostLockedSet> result)
    {
        for (int row = start; row < 9; row++)
        {
            if (strategyManager.Sudoku[row, col] != 0) continue;

            IPossibilities mashed = current.Or(strategyManager.PossibilitiesAt(row, col));
            if (mashed.Count == current.Count + strategyManager.PossibilitiesAt(row, col).Count) continue;

            var copy = new List<Cell>(visited) { new (row, col) };

            if (mashed.Count == copy.Count + 1)
            {
                result.Add(new AlmostLockedSet(copy.ToArray(), mashed));
            }

            SearchColumn(strategyManager, col, row + 1, mashed, copy, result);
        }
    }

    private void SearchMiniGrid(IStrategyManager strategyManager, int miniRow, int miniCol, int start,
        IReadOnlyPossibilities current, List<Cell> visited, List<AlmostLockedSet> result)
    {
        for (int n = start; n < 9; n++)
        {
            int row = miniRow * 3 + n / 3;
            int col = miniCol * 3 + n % 3;
                
            if (strategyManager.Sudoku[row, col] != 0) continue;

            IPossibilities mashed = current.Or(strategyManager.PossibilitiesAt(row, col));
            if (mashed.Count == current.Count + strategyManager.PossibilitiesAt(row, col).Count) continue;

            var copy = new List<Cell>(visited) { new (row, col) };

            if (mashed.Count == copy.Count + 1 && NotInSameRowOrColumn(copy))
            {
                result.Add(new AlmostLockedSet(copy.ToArray(), mashed));
            }

            SearchMiniGrid(strategyManager, miniRow, miniCol, n + 1, mashed,
                copy, result);
        }
    }

    private bool NotInSameRowOrColumn(List<Cell> coord)
    {
        int row = coord[0].Row;
        int col = coord[0].Col;

        bool rowOk = false;
        bool colOk = false;

        for (int i = 1; i < coord.Count; i++)
        {
            if (!rowOk && coord[i].Row != row) rowOk = true;
            if (!colOk && coord[i].Col != col) colOk = true;

            if (rowOk && colOk) return true;
        }

        return false;
    }

    private Dictionary<ILinkGraphElement, Coloring> DoColor(ILinkGraphElement start, Coloring firstColor)
    {
        var graph = LinkGraph();

        Queue<ILinkGraphElement> queue = new();
        queue.Enqueue(start);

        Dictionary<ILinkGraphElement, Coloring> result = new();
        result.Add(start, firstColor);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            Coloring opposite = result[current] == Coloring.On ? Coloring.Off : Coloring.On;

            foreach (var friend in graph.GetLinks(current, LinkStrength.Strong))
            {
                if (!result.ContainsKey(friend))
                {
                    result[friend] = opposite;
                    queue.Enqueue(friend);
                }
            }

            if (opposite == Coloring.Off)
            {
                foreach (var friend in graph.GetLinks(current, LinkStrength.Weak))
                {
                    if (!result.ContainsKey(friend))
                    {
                        result[friend] = opposite;
                        queue.Enqueue(friend);
                    }
                }
            }
            else if (current is CellPossibility pos)
            {
                CellPossibility? row = null;
                bool rowB = true;
                CellPossibility? col = null;
                bool colB = true;
                CellPossibility? mini = null;
                bool miniB = true;
            
                foreach (var friend in graph.GetLinks(current, LinkStrength.Weak))
                {
                    if (friend is not CellPossibility friendPos) continue;
                    if (rowB && friendPos.Row == pos.Row)
                    {
                        if (result.TryGetValue(friend, out var coloring))
                        {
                            if (coloring == Coloring.On) rowB = false;
                        }
                        else
                        {
                            if (row is null) row = friendPos;
                            else rowB = false;  
                        }
                    
                    }

                    if (colB && friendPos.Col == pos.Col)
                    {
                        if (result.TryGetValue(friend, out var coloring))
                        {
                            if (coloring == Coloring.On) colB = false;
                        }
                        else
                        {
                            if (col is null) col = friendPos;
                            else colB = false;
                        }
                    }

                    if (miniB && friendPos.Row / 3 == pos.Row / 3 && friendPos.Col / 3 == pos.Col / 3)
                    {
                        if (result.TryGetValue(friend, out var coloring))
                        {
                            if (coloring == Coloring.On) miniB = false;
                        }
                        else
                        {
                            if (mini is null) mini = friendPos;
                            else miniB = false;
                        }
                    }
                }

                if (row is not null && rowB)
                {
                    result[row] = Coloring.On;
                    queue.Enqueue(row);
                }

                if (col is not null && colB)
                {
                    result[col] = Coloring.On;
                    queue.Enqueue(col);
                }

                if (mini is not null && miniB)
                {
                    result[mini] = Coloring.On;
                    queue.Enqueue(mini);
                }
            }
        }

        return result;
    }
}