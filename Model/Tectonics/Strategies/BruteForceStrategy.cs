﻿using Model.Helpers;
using Model.Helpers.Changes;
using Model.Helpers.Highlighting;
using Model.Sudokus.Solver;
using Model.Utility;

namespace Model.Tectonics.Strategies;

public class BruteForceStrategy : TectonicStrategy
{
    public BruteForceStrategy() : base("Brute Force", StepDifficulty.ByTrial, InstanceHandling.FirstOnly)
    {
    }

    public override void Apply(ITectonicStrategyUser strategyUser)
    {
        var solution = BackTracking.Fill(strategyUser.Tectonic.Copy(), strategyUser, 1);
        if (solution.Count != 1) return;

        for (int row = 0; row < strategyUser.Tectonic.RowCount; row++)
        {
            for (int col = 0; col < strategyUser.Tectonic.ColumnCount; col++)
            {
                strategyUser.ChangeBuffer.ProposeSolutionAddition(solution[0][row, col], row, col);
            }
        }
        
        strategyUser.ChangeBuffer.Commit(DefaultChangeReportBuilder<IUpdatableTectonicSolvingState, ITectonicHighlighter>.Instance);
    }
}