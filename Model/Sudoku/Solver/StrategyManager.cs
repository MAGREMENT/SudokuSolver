﻿using System.Collections.Generic;
using Model.Utility;

namespace Model.Sudoku.Solver;

public class StrategyManager
{
    public bool UniquenessDependantStrategiesAllowed { get; private set; } = true;
    private readonly UniqueList<SudokuStrategy> _strategies = new();
    public IReadOnlyList<SudokuStrategy> Strategies => _strategies;

    public void ClearStrategies()
    {
        _strategies.Clear();
    }
    
    public void AddStrategy(SudokuStrategy strategy)
    {
        _strategies.Add(strategy, i => InterchangeStrategies(i, _strategies.Count));
    }

    public void AddStrategy(SudokuStrategy strategy, int position)
    {
        if (position == _strategies.Count)
        {
            AddStrategy(strategy);
            return;
        }

        _strategies.InsertAt(strategy, position, i => InterchangeStrategies(i, position));
    }
    
    public void AddStrategies(IReadOnlyList<SudokuStrategy>? strategies)
    {
        if (strategies is null) return;
        
        foreach (var s in strategies)
        {
            _strategies.Add(s);
        }
    }
    
    public void RemoveStrategy(int position)
    {
        _strategies.RemoveAt(position);
    }
    
    public void InterchangeStrategies(int positionOne, int positionTwo)
    {
        var buffer = _strategies[positionOne];
        _strategies.RemoveAt(positionOne);
        var newPosTwo = positionTwo > positionOne ? positionTwo - 1 : positionTwo;
        AddStrategy(buffer, newPosTwo);
    }
}