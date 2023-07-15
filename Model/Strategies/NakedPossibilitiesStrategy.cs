﻿using System.Collections.Generic;
using Model.Possibilities;

namespace Model.Strategies;

public class NakedPossibilitiesStrategy : IStrategy
{
    private readonly int _type;

    public NakedPossibilitiesStrategy(int type)
    {
        _type = type;
    }
    
    
    public void ApplyOnce(ISolver solver)
    {
        //Rows
        for (int row = 0; row < 9; row++)
        {
            IPossibilities empty = new BoolArrayPossibilities();
            empty.RemoveAll();
            var possibleCols = EveryRowCellWithLessPossibilities(solver, row, _type + 1);
            RecursiveRowMashing(solver, empty, possibleCols, row, _type, new HashSet<int>());
        }
        
        //Cols
        for (int col = 0; col < 9; col++)
        {
            IPossibilities empty = new BoolArrayPossibilities();
            empty.RemoveAll();
            var possibleRows = EveryColumnCellWithLessPossibilities(solver, col, _type + 1);
            RecursiveColumnMashing(solver, empty, possibleRows, col, _type, new HashSet<int>());
        }
        
        //MiniGrid
        for (int miniRow = 0; miniRow < 3; miniRow++)
        {
            for (int miniCol = 0; miniCol < 3; miniCol++)
            {
                IPossibilities empty = new BoolArrayPossibilities();
                empty.RemoveAll();
                var possibleGridNumbers = EveryMiniGridCellWithLessPossibilities(solver, miniRow, miniCol, _type + 1);
                RecursiveMiniGridMashing(solver, empty, possibleGridNumbers, miniRow, miniCol, _type, new HashSet<int>());
            }
        }
    }
    
    private Queue<int> EveryRowCellWithLessPossibilities(ISolver solver, int row, int than)
    {
        Queue<int> result = new();
        for (int col = 0; col < 9; col++)
        {
            if (solver.Sudoku[row, col] == 0 && solver.Possibilities[row, col].Count < than) 
                result.Enqueue(col);
        }

        return result;
    }

    private void RecursiveRowMashing(ISolver solver, IPossibilities current,
        Queue<int> possibleCols, int row, int count, HashSet<int> visited)
    {
        while (possibleCols.Count > 0)
        {
            int col = possibleCols.Dequeue();

            IPossibilities newCurrent = current.Mash(solver.Possibilities[row, col]);
            var newVisited = new HashSet<int>(visited) { col };
            
            if (newCurrent.Count <= _type)
            {
                if (count - 1 == 0 && newCurrent.Count == _type)
                {
                    if (_type == 1) solver.AddDefinitiveNumber(newCurrent.GetFirst(), row, col,
                        new NakedPossibilityLog(row, col, newCurrent.GetFirst(), _type));
                    else RemovePossibilitiesFromRow(solver, row, newCurrent, newVisited);
                }
                else
                {
                    RecursiveRowMashing(solver, newCurrent,
                        new Queue<int>(possibleCols), row, count - 1, newVisited);
                }
            }
        }
    }

    private void RemovePossibilitiesFromRow(ISolver solver, int row, IPossibilities toRemove, HashSet<int> except)
    {
        foreach (var n in toRemove)
        {
            for (int col = 0; col < 9; col++)
            {
                if (solver.Sudoku[row, col] == 0 && !except.Contains(col))
                    solver.RemovePossibility(n, row, col, new NakedPossibilityLog(row, col, n, _type));
            }
        }
    }
    
    private Queue<int> EveryColumnCellWithLessPossibilities(ISolver solver, int col, int than)
    {
        Queue<int> result = new();
        for (int row = 0; row < 9; row++)
        {
            if (solver.Sudoku[row, col] == 0 && solver.Possibilities[row, col].Count < than) 
                result.Enqueue(row);
        }

        return result;
    }
    
    private void RecursiveColumnMashing(ISolver solver, IPossibilities current,
        Queue<int> possibleRows, int col, int count, HashSet<int> visited)
    {
        while(possibleRows.Count > 0)
        {
            int row = possibleRows.Dequeue();

            IPossibilities newCurrent = current.Mash(solver.Possibilities[row, col]);
            var newVisited = new HashSet<int>(visited) { row };
            
            if (newCurrent.Count <= _type)
            {
                if (count - 1 == 0 && newCurrent.Count == _type)
                {
                    if (_type == 1) solver.AddDefinitiveNumber(newCurrent.GetFirst(), row, col,
                        new NakedPossibilityLog(row, col, newCurrent.GetFirst(), _type));
                    else RemovePossibilitiesFromColumn(solver, col, newCurrent, newVisited);
                }
                else
                {
                    RecursiveColumnMashing(solver, newCurrent, new Queue<int>(possibleRows),
                        col, count - 1, newVisited);
                }
            }
        }
    }

    private void RemovePossibilitiesFromColumn(ISolver solver, int col, IPossibilities toRemove, HashSet<int> except)
    {
        foreach (var n in toRemove)
        {
            for (int row = 0; row < 9; row++)
            {
                if (solver.Sudoku[row, col] == 0 && !except.Contains(row))
                    solver.RemovePossibility(n, row, col, new NakedPossibilityLog(row, col, n, _type));
            }
        }
    }
    
    private Queue<int> EveryMiniGridCellWithLessPossibilities(ISolver solver, int miniRow, int miniCol, int than)
    {
        Queue<int> result = new();
        for (int gridNumber = 0; gridNumber < 9; gridNumber++)
        {
            int row = miniRow * 3 + gridNumber / 3;
            int col = miniCol * 3 + gridNumber % 3;
            
            if (solver.Sudoku[row, col] == 0 && solver.Possibilities[row, col].Count < than) 
                result.Enqueue(gridNumber);
        }
        
        return result;
    }
    
    private void RecursiveMiniGridMashing(ISolver solver, IPossibilities current,
        Queue<int> possibleGridNumbers, int miniRow, int miniCol, int count, HashSet<int> visited)
    {
        while(possibleGridNumbers.Count > 0)
        {
            int gridNumber = possibleGridNumbers.Dequeue();
            
            int row = miniRow * 3 + gridNumber / 3;
            int col = miniCol * 3 + gridNumber % 3;
            
            IPossibilities newCurrent = current.Mash(solver.Possibilities[row, col]);
            var newVisited = new HashSet<int>(visited) { gridNumber };

            if (newCurrent.Count <= _type)
            {
                if (count - 1 == 0 && newCurrent.Count == _type)
                {
                    if (_type == 1) solver.AddDefinitiveNumber(newCurrent.GetFirst(), row, col,
                        new NakedPossibilityLog(row, col, newCurrent.GetFirst(), _type));
                    else RemovePossibilitiesFromMiniGrid(solver, miniRow, miniCol, newCurrent, newVisited);
                }
                else
                {
                    RecursiveMiniGridMashing(solver, newCurrent, new Queue<int>(possibleGridNumbers),
                        miniRow, miniCol, count - 1, newVisited);
                }
            }
        }
    }
    
    private void RemovePossibilitiesFromMiniGrid(ISolver solver, int miniRow, int miniCol, IPossibilities toRemove,
        HashSet<int> except)
    {
        foreach (var n in toRemove)
        {
            for (int gridNumber = 0; gridNumber < 9; gridNumber++)
            {
                int row = miniRow * 3 + gridNumber / 3;
                int col = miniCol * 3 + gridNumber % 3;
                
                if (solver.Sudoku[row, col] == 0 && !except.Contains(gridNumber))
                    solver.RemovePossibility(n, row, col, new NakedPossibilityLog(row, col, n, _type));
            }
        }
    }
}

public class NakedPossibilityLog : ISolverLog
{
    public string AsString { get; }
    public StrategyLevel Level { get; }

    public NakedPossibilityLog(int row, int col, int number, int type)
    {
        if (type == 1)
        {
            AsString = $"[{row + 1}, {col + 1}] {number} added as definitive because of naked {type}";
            Level = StrategyLevel.Basic;
        }
        else
        {
            AsString = $"[{row + 1}, {col + 1}] {number} removed from possibilities because of naked {type}";
            Level = (StrategyLevel) type;
        }
        
    }
}