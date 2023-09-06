﻿using System.Collections.Generic;
using System.Linq;
using Model.Changes;
using Model.Solver;

namespace Model.Strategies;

public class HiddenSingleStrategy : IStrategy
{
    public string Name => "Hidden single";
    public StrategyLevel Difficulty => StrategyLevel.Basic;
    public StatisticsTracker Tracker { get; } = new();
    public void ApplyOnce(IStrategyManager strategyManager)
    {
        for (int number = 1; number <= 9; number++)
        {
            for (int row = 0; row < 9; row++)
            {
                var ppir = strategyManager.RowPositions(row, number);
                if (ppir.Count == 1) strategyManager.ChangeBuffer.AddDefinitiveToAdd(number, row, ppir.First());
            }

            for (int col = 0; col < 9; col++)
            {
                var ppic = strategyManager.ColumnPositions(col, number);
                if (ppic.Count == 1) strategyManager.ChangeBuffer.AddDefinitiveToAdd(number, ppic.First(), col);
            }

            for (int miniRow = 0; miniRow < 3; miniRow++)
            {
                for (int miniCol = 0; miniCol < 3; miniCol++)
                {
                    var ppimn = strategyManager.MiniGridPositions(miniRow, miniCol, number);
                    if (ppimn.Count == 1)
                    {
                        var pos = ppimn.First();
                        strategyManager.ChangeBuffer.AddDefinitiveToAdd(number, pos.Row, pos.Col);
                    }
                }
            }
        }

        strategyManager.ChangeBuffer.Push(this, new HiddenSingleReportBuilder());
    }
}

public class HiddenSingleReportBuilder : IChangeReportBuilder
{
    public ChangeReport Build(List<SolverChange> changes, IChangeManager manager)
    {
        return new ChangeReport(IChangeReportBuilder.ChangesToString(changes),
            "The numbers were added for being the only one in their unit",
            lighter => IChangeReportBuilder.HighlightChanges(lighter, changes));
    }
}