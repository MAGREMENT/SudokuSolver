﻿using System;
using System.Collections.Generic;
using System.Text;
using Model.Changes;
using Model.Solver;

namespace Model.Strategies;

/// <summary>
/// A hidden single is a unit that contain a possibility in only one of its cells. Since every unit must contain
/// every number once, the possibility is the solution to that cell.
/// </summary>
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
                if (ppir.Count == 1) strategyManager.ChangeBuffer.AddDefinitiveToAdd(number, row, ppir.GetFirst());
            }

            for (int col = 0; col < 9; col++)
            {
                var ppic = strategyManager.ColumnPositions(col, number);
                if (ppic.Count == 1) strategyManager.ChangeBuffer.AddDefinitiveToAdd(number, ppic.GetFirst(), col);
            }

            for (int miniRow = 0; miniRow < 3; miniRow++)
            {
                for (int miniCol = 0; miniCol < 3; miniCol++)
                {
                    var ppimn = strategyManager.MiniGridPositions(miniRow, miniCol, number);
                    if (ppimn.Count != 1) continue;
                    
                    var pos = ppimn.GetFirst();
                    strategyManager.ChangeBuffer.AddDefinitiveToAdd(number, pos.Row, pos.Col);
                }
            }
        }

        strategyManager.ChangeBuffer.Push(this, new HiddenSingleReportBuilder());
    }
}

public class HiddenSingleReportBuilder : IChangeReportBuilder
{
    public ChangeReport Build(List<SolverChange> changes, IPossibilitiesHolder snapshot)
    {
        return new ChangeReport(IChangeReportBuilder.ChangesToString(changes), ChangesToExplanation(changes, snapshot),
            lighter => IChangeReportBuilder.HighlightChanges(lighter, changes));
    }

    private string ChangesToExplanation(List<SolverChange> changes, IPossibilitiesHolder snapshot)
    {
        var builder = new StringBuilder();

        foreach (var change in changes)
        {
            string where;
            if (snapshot.RowPositions(change.Row, change.Number).Count == 1) where = $"row {change.Row + 1}";
            else if (snapshot.ColumnPositions(change.Column, change.Number).Count == 1)
                where = $"column {change.Column + 1}";
            else
            {
                var miniGridPositions = snapshot.MiniGridPositions(change.Row / 3, change.Column / 3, change.Number);
                if (miniGridPositions.Count == 1) where = $"mini grid {miniGridPositions.MiniGridNumber()}";
                else throw new Exception("Error while backtracking hidden singles");
            }

            builder.Append($"{change.Number} is the solution to the cell [{change.Row + 1}, {change.Column + 1}]" +
                           $" because it's present only there in {where}.\n");
        }

        return builder.ToString();
    }

}