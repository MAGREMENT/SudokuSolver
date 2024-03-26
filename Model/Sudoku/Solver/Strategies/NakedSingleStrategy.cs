﻿using System.Collections.Generic;
using Model.Helpers;
using Model.Helpers.Highlighting;
using Model.Helpers.Changes;

namespace Model.Sudoku.Solver.Strategies;

public class NakedSingleStrategy : SudokuStrategy
{
    public const string OfficialName = "Naked Single";
    private const InstanceHandling DefaultInstanceHandling = InstanceHandling.UnorderedAll;

    public NakedSingleStrategy() : base(OfficialName, StrategyDifficulty.Basic, DefaultInstanceHandling) {}
    
    public override void Apply(IStrategyUser strategyUser)
    {
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                if (strategyUser.PossibilitiesAt(row, col).Count != 1) continue;
                
                strategyUser.ChangeBuffer.ProposeSolutionAddition(strategyUser.PossibilitiesAt(row, col).FirstPossibility(), row, col);
                strategyUser.ChangeBuffer.Commit( new NakedSingleReportBuilder());
                if (StopOnFirstPush) return;
            }
        }
    }
}

public class NakedSingleReportBuilder : IChangeReportBuilder<IUpdatableSudokuSolvingState, ISudokuHighlighter>
{
    public ChangeReport<ISudokuHighlighter> Build(IReadOnlyList<SolverProgress> changes, IUpdatableSudokuSolvingState snapshot)
    {
        return new ChangeReport<ISudokuHighlighter>( Description(changes),
            lighter => ChangeReportHelper.HighlightChanges(lighter, changes));
    }

    private static string Description(IReadOnlyList<SolverProgress> changes)
    {
        return changes.Count != 1 ? "" : $"Naked Single in r{changes[0].Row + 1}c{changes[0].Column + 1}";
    }
}