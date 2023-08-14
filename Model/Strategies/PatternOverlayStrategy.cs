﻿using System.Collections.Generic;
using Model.Positions;
using Model.StrategiesUtil;

namespace Model.Strategies;

public class PatternOverlayStrategy : IStrategy //TODO debug pattern detection
{
    public string Name => "Pattern Overlay";
    public StrategyLevel Difficulty => StrategyLevel.Extreme;
    public int Score { get; set; }

    public void ApplyOnce(IStrategyManager strategyManager)
    {
        var all = AllPositionsOfAllNumbers(strategyManager);

        List<Pattern>[] patterns = new List<Pattern>[9];
        for (int i = 0; i < patterns.Length; i++)
        {
            patterns[i] = Patterns(strategyManager, all[i], i + 1);
        }

        for (int i = 0; i < all.Length; i++)
        {
            foreach (var coord in all[i])
            {
                int count = 0;
                foreach (var pattern in patterns[i])
                {
                    if (pattern.Peek(coord)) count++;
                }
                
                if(count == 0) strategyManager.ChangeBuffer.AddPossibilityToRemove(i + 1, coord.Row, coord.Col);
                else if(count == patterns[i].Count) strategyManager.ChangeBuffer.AddDefinitiveToAdd(i + 1, coord.Row, coord.Col);
            }

            if (strategyManager.ChangeBuffer.NotEmpty())
                strategyManager.ChangeBuffer.Push(this, new PatternOverlayReportBuilder());
        }
        
        //TODO add rule 2
    }

    private List<Coordinate>[] AllPositionsOfAllNumbers(IStrategyManager strategyManager)
    {
        List<Coordinate>[] possiblePositions =
        {
            new(), new(), new(),
            new(), new(), new(),
            new(), new(), new()
        };

        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                if (strategyManager.Sudoku[row, col] != 0) continue;

                Coordinate current = new Coordinate(row, col);
                foreach (var possibility in strategyManager.Possibilities[row, col])
                {
                    possiblePositions[possibility - 1].Add(current);
                }
            }
        }

        return possiblePositions;
    }

    private List<Pattern> Patterns(IStrategyManager strategyManager, List<Coordinate> all, int number)
    {
        List<Pattern> result = new List<Pattern>();

        SearchForPatterns(strategyManager, all, number, new GridPositions(), result);

        return result;
    }

    private void SearchForPatterns(IStrategyManager strategyManager, List<Coordinate> toSearch, int number,
        GridPositions buildup, List<Pattern> result)
    {
        foreach (var current in toSearch)
        {
            buildup.Add(current);
            var copy = new List<Coordinate>(toSearch);
            copy.RemoveAll(coord => coord.Row == current.Row || coord.Col == current.Col ||
                                    (coord.Row / 3 == current.Row / 3 && coord.Col / 3 == current.Col / 3));

            if (copy.Count == 0)
            {
                for (int row = 0; row < 9; row++)
                {
                    if (strategyManager.Sudoku.RowCount(row, number) + buildup.RowCount(row) != 1) return;
                }

                for (int col = 0; col < 9; col++)
                {
                    if (strategyManager.Sudoku.ColumnCount(col, number) + buildup.ColumnCount(col) != 1) return;
                }

                for (int miniRow = 0; miniRow < 3; miniRow++)
                {
                    for (int miniCol = 0; miniCol < 3; miniCol++)
                    {
                        if (strategyManager.Sudoku.MiniGridCount(miniRow, miniCol, number) +
                            buildup.MiniGridCount(miniRow, miniCol) != 1) return;
                    }
                }
                
                result.Add(new Pattern(buildup));
            }
            else
            {
                SearchForPatterns(strategyManager, copy, number, buildup.Copy(), result);
            }
        }
    }

}

public class Pattern
{
    private readonly GridPositions _pattern;
    public Pattern(GridPositions positions)
    {
        _pattern = positions;
    }

    public bool Peek(Coordinate coordinate)
    {
        return _pattern.Peek(coordinate);
    }
}

public class PatternOverlayReportBuilder : IChangeReportBuilder
{
    public ChangeReport Build(List<SolverChange> changes, IChangeManager manager)
    {
        return new ChangeReport(IChangeReportBuilder.ChangesToString(changes),
            lighter => IChangeReportBuilder.HighlightChanges(lighter, changes), "");
    }
}