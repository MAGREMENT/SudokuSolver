﻿using System.Collections.Generic;
using Model.Sudoku.Solver.Helpers.Changes;
using Model.Sudoku.Solver.Position;

namespace Model.Sudoku.Solver.Strategies;

public class BruteForceStrategy : AbstractStrategy
{
    public const string OfficialName = "Brute Force";
    private const OnCommitBehavior DefaultBehavior = OnCommitBehavior.WaitForAll;
    
    public override OnCommitBehavior DefaultOnCommitBehavior => DefaultBehavior;
    
    public BruteForceStrategy() : base(OfficialName, StrategyDifficulty.ByTrial, DefaultBehavior) { }

    public override void Apply(IStrategyUser strategyUser)
    {
        var positions = new GridPositions[] { new(), new(), new(), new(), new(), new(), new(), new(), new() };

        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                var n = strategyUser.Sudoku[row, col];
                if (n == 0) continue;

                positions[n - 1].Add(row, col);
            }
        }
        
        Search(strategyUser, strategyUser.Sudoku.Copy(), positions, 0);
    }

    private bool Search(IStrategyUser strategyUser, Sudoku s, GridPositions[] positions, int current)
    {
        if (current == 81)
        {
            Process(strategyUser, s);
            return true;
        }
        
        var row = current / 9;
        var col = current % 9;

        var possibilities = strategyUser.PossibilitiesAt(row, col);
        if (possibilities.Count == 0)
        {
            if (Search(strategyUser, s, positions, current + 1)) return true;
        }

        foreach (var possibility in possibilities.EnumeratePossibilities())
        {
            var pos = positions[possibility - 1];
            if(pos.RowCount(row) > 0 || pos.ColumnCount(col) > 0
                                     || pos.MiniGridCount(row / 3, col / 3) > 0) continue;
                
            s[row, col] = possibility;
            pos.Add(row, col);
                
            if (Search(strategyUser, s, positions, current + 1)) return true;
                
            s[row, col] = 0;
            pos.Remove(row, col);
        }

        return false;
    }

    private void Process(IStrategyUser strategyUser, Sudoku s)
    {
        for (int r = 0; r < 9; r++)
        {
            for (int c = 0; c < 9; c++)
            {
                if(strategyUser.Sudoku[r, c] != 0) continue;
                    
                strategyUser.ChangeBuffer.ProposeSolutionAddition(s[r, c], r, c);
            }
        }

        strategyUser.ChangeBuffer.Commit(this, new BruteForceReportBuilder());
    }
}

public class BruteForceReportBuilder : IChangeReportBuilder
{
    public ChangeReport Build(IReadOnlyList<SolverChange> changes, IPossibilitiesHolder snapshot)
    {
        return ChangeReport.Default(changes);
    }
}