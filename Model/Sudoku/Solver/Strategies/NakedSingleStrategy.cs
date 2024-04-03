﻿using System.Collections.Generic;
using Model.Helpers;
using Model.Helpers.Highlighting;
using Model.Helpers.Changes;
using Model.Helpers.Logs;
using Model.Sudoku.Solver.Explanation;
using Model.Utility;

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

    public override SudokuClue? TransformIntoClue(BuiltChangeCommit<ISudokuHighlighter> commit)
    {
        if (commit.Changes.Length != 1) return null;
        
        var change = commit.Changes[0];
        return new SudokuClue(lighter =>
        {
            lighter.EncircleCell(change.Row, change.Column);
        }, $"Look at the possibilities in r{change.Row + 1}c{change.Column + 1}");
    }
}

public class NakedSingleReportBuilder : IChangeReportBuilder<IUpdatableSudokuSolvingState, ISudokuHighlighter>
{
    public ChangeReport<ISudokuHighlighter> Build(IReadOnlyList<SolverProgress> changes, IUpdatableSudokuSolvingState snapshot)
    {
        return new ChangeReport<ISudokuHighlighter>(Description(changes),
            lighter => ChangeReportHelper.HighlightChanges(lighter, changes), Explanation(changes));
    }

    private static string Description(IReadOnlyList<SolverProgress> changes)
    {
        return changes.Count != 1 ? "" : $"Naked Single in r{changes[0].Row + 1}c{changes[0].Column + 1}";
    }

    private static ExplanationElement? Explanation(IReadOnlyList<SolverProgress> changes)
    {
        if (changes.Count != 1) return null;

        var change = changes[0];
        var start = new StringExplanationElement($"{change.Number} is the only possibility for ");
        _ = start + new Cell(change.Row, change.Column) + ". It is therefor the solution for that cell.";

        return start;
    }
}