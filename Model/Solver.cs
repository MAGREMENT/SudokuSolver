﻿using System.Collections.Generic;
using Model.Strategies.IHaveToFindBetterNames;
using Model.Strategies.SamePossibilities;
using Model.Strategies.SinglePossibility;

namespace Model;

public class Solver : ISolver
{
    public CellPossibilities[,] Possibilities { get; init; }
    public Sudoku Sudoku { get; }

    private readonly SolverStrategyPackage[] _strategies =
    {
        new SinglePossibilityStrategyPackage(),
        new SamePossibilitiesStrategyPackage(),
        new GroupedPossibilitiesStrategyPackage()
    };
    
    public delegate void OnNumberAdded(int row, int col);
    public event OnNumberAdded? NumberAdded;

    public delegate void OnPossibilityRemoved(int row, int col);
    public event OnPossibilityRemoved? PossibilityRemoved;

    private List<int[]> _listOfChanges = new();

    public Solver(Sudoku s)
    {
        Possibilities = new CellPossibilities[9, 9];
        Sudoku = s;

        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                Possibilities[i, j] = new CellPossibilities();
            }
        }
        
        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                if (Sudoku[i, j] != 0)
                {
                    UpdatePossibilitiesAfterDefinitiveNumberAdded(s[i, j], i, j);
                }
            }
        }
    }
    
    public bool AddDefinitiveNumber(int number, int row, int col)
    {
        if (Sudoku[row, col] != 0) return false;
        Sudoku[row, col] = number;
        UpdatePossibilitiesAfterDefinitiveNumberAdded(number, row, col);
        NumberAdded?.Invoke(row, col);
        return true;
    }

    public bool RemovePossibility(int possibility, int row, int col)
    {
        bool buffer = Possibilities[row, col].Remove(possibility);
        if(buffer) PossibilityRemoved?.Invoke(row, col);
        return buffer;
    }

    private void UpdatePossibilitiesAfterDefinitiveNumberAdded(int number, int row, int col)
    {
        for (int i = 0; i < 9; i++)
        {
            Possibilities[row, i].Remove(number);
            Possibilities[i, col].Remove(number);
        }
        
        int startRow = (row / 3) * 3;
        int startColumn = (col / 3) * 3;
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                Possibilities[startRow + i, startColumn + j].Remove(number);
            }
        }
        
        
    }

    public void Solve()
    {
        for (int i = 0; i < _strategies.Length; i++)
        {
            if (Sudoku.IsComplete()) return;
            if (_strategies[i].ApplyAllOnce(this)) i = -1;
        }
    }

    public void RunAllStrategiesOnce()
    {
        foreach (var strategy in _strategies)
        {
            strategy.ApplyAllOnce(this);
        }
    }

    public List<int[]> RunUntilProgress()
    {
        _listOfChanges.Clear();
        NumberAdded += AddToListOfChanges;
        PossibilityRemoved += AddToListOfChanges;
        
        foreach (var strategy in _strategies)
        {
            if (strategy.ApplyUntilProgress(this)) break;
        }
        
        NumberAdded -= AddToListOfChanges;
        PossibilityRemoved -= AddToListOfChanges;
        
        return _listOfChanges;
    }

    private void AddToListOfChanges(int row, int col)
    {
        _listOfChanges.Add(new[] {row, col});
    }
    
    

}

