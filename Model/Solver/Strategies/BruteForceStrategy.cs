﻿using System.Collections.Generic;
using Model.Solver.Helpers.Changes;
using Model.Solver.Positions;

namespace Model.Solver.Strategies;

public class BruteForceStrategy : AbstractStrategy
{
    public const string OfficialName = "Brute Force";
    
    public BruteForceStrategy() : base(OfficialName, StrategyDifficulty.ByTrial) { }

    public override void ApplyOnce(IStrategyManager strategyManager)
    {
        var positions = new GridPositions[] { new(), new(), new(), new(), new(), new(), new(), new(), new() };

        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                var n = strategyManager.Sudoku[row, col];
                if (n == 0) continue;

                positions[n - 1].Add(row, col);
            }
        }
        
        Search(strategyManager, strategyManager.Sudoku.Copy(), positions, 0);
    }

    private bool Search(IStrategyManager strategyManager, Sudoku s, GridPositions[] positions, int current)
    {
        if (current == 81)
        {
            Process(strategyManager, s);
            return true;
        }
        
        var row = current / 9;
        var col = current % 9;

        var possibilities = strategyManager.PossibilitiesAt(row, col);
        if (possibilities.Count == 0)
        {
            if (Search(strategyManager, s, positions, current + 1)) return true;
        }

        foreach (var possibility in possibilities)
        {
            var pos = positions[possibility - 1];
            if(pos.RowCount(row) > 0 || pos.ColumnCount(col) > 0
                                     || pos.MiniGridCount(row / 3, col / 3) > 0) continue;
                
            s[row, col] = possibility;
            pos.Add(row, col);
                
            if (Search(strategyManager, s, positions, current + 1)) return true;
                
            s[row, col] = 0;
            pos.Remove(row, col);
        }

        return false;
    }

    private void Process(IStrategyManager strategyManager, Sudoku s)
    {
        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                if(strategyManager.Sudoku[r, c] != 0) continue;
                    
                strategyManager.ChangeBuffer.AddSolutionToAdd(s[r, c], r, c);
            }
        }

        strategyManager.ChangeBuffer.Push(this, new BruteForceReportBuilder());
    }
}

public class BruteForceReportBuilder : IChangeReportBuilder
{
    public ChangeReport Build(List<SolverChange> changes, IPossibilitiesHolder snapshot)
    {
        return ChangeReport.Default(changes);
    }
}