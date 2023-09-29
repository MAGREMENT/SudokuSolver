﻿using System.Collections.Generic;
using Model.Solver.Helpers.Changes;
using Model.Solver.Possibilities;
using Model.Solver.StrategiesUtil;

namespace Model.Solver.Strategies;

public class SKLoopsStrategy : AbstractStrategy
{
    public const string OfficialName = "SK-Loops";
    
    public SKLoopsStrategy() : base(OfficialName, StrategyDifficulty.Hard) { }

    public override void ApplyOnce(IStrategyManager strategyManager)
    {
        for (int miniCol = 0; miniCol < 2; miniCol++)
        {
            for (int gridCol = 0; gridCol < 3; gridCol++)
            {
                for (int miniRow = 0; miniRow < 2; miniRow++)
                {
                    for (int gridRow = 0; gridRow < 3; gridRow++)
                    {
                        int row = miniRow * 3 + gridRow;
                        int col = miniCol * 3 + gridCol;

                        if (strategyManager.Sudoku[row, col] == 0 || !IsCellValid(strategyManager, row, col)) continue;

                        for (int nextMiniCol = miniCol + 1; nextMiniCol < 3; nextMiniCol++)
                        {
                            for (int nextGridCol = 0; nextGridCol < 3; nextGridCol++)
                            {
                                int nextCol = nextMiniCol * 3 + nextGridCol;
                                
                                if(strategyManager.Sudoku[row, nextCol] == 0 || !IsCellValid(strategyManager, row, nextCol)) continue;

                                for (int nextMiniRow = miniRow + 1; nextMiniRow < 3; nextMiniRow++)
                                {
                                    for (int nextGridRow = 0; nextGridRow < 3; nextGridRow++)
                                    {
                                        int nextRow = nextMiniRow * 3 + nextGridRow;

                                        if (strategyManager.Sudoku[nextRow, col] == 0
                                            || strategyManager.Sudoku[nextRow, nextCol] == 0
                                            || !IsCellValid(strategyManager, nextRow, col)
                                            || !IsCellValid(strategyManager, nextRow, nextCol)) continue;

                                        ConfirmPattern(strategyManager, new Cell(row, col), new Cell(row, nextCol),
                                            new Cell(nextRow, nextCol), new Cell(nextRow, col));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private void ConfirmPattern(IStrategyManager strategyManager, params Cell[] cells)
    {
        var one = CrossRowPossibilities(strategyManager, cells[0]);
        var two = CrossRowPossibilities(strategyManager, cells[1]);
        var and = one.Possibilities.And(two.Possibilities);
        if (and.Count == 0) return;

        if (!and.Equals(one.Possibilities) && !and.Equals(two.Possibilities)) IsLoop(strategyManager, cells, and);
        
        if (and.Count == 1) return;
        
        foreach (var combination in EachCombination(and))
        {
            IsLoop(strategyManager, cells, combination);
        }
    }

    private static List<IPossibilities> EachCombination(IPossibilities possibilities)
    {
        List<IPossibilities> result = new();
        EachCombination(result, possibilities, IPossibilities.NewEmpty());
        return result;
    }

    private static void EachCombination(List<IPossibilities> result, IPossibilities total, IPossibilities toSearch)
    {
        foreach (var possibility in total)
        {
            if (toSearch.Peek(possibility)) continue;

            toSearch.Add(possibility);
            result.Add(toSearch.Copy());
            EachCombination(result, total, toSearch);
            toSearch.Remove(possibility);
        }
    }
    
    private void IsLoop(IStrategyManager strategyManager, Cell[] cells, IPossibilities start)
    {
        int possibilityCount = 0;
        int cellCount = 0;
        IPossibilities[] links = new IPossibilities[8];
        int cursor = 0;
        
        var second = CrossRowPossibilities(strategyManager, cells[1]);
        second.Possibilities.Remove(start);
        if (second.Possibilities.Count == 0) return;
        links[cursor++] = second.Possibilities;
        possibilityCount += second.Possibilities.Count;
        cellCount += second.Number;

        var third = CrossColPossibilities(strategyManager, cells[1]);
        if (!third.Possibilities.PeekAll(second.Possibilities)) return;
        third.Possibilities.Remove(second.Possibilities);
        if (third.Possibilities.Count == 0) return;
        links[cursor++] = third.Possibilities;
        possibilityCount += third.Possibilities.Count;
        cellCount += third.Number;

        var fourth = CrossColPossibilities(strategyManager, cells[2]);
        if (!fourth.Possibilities.PeekAll(third.Possibilities)) return;
        fourth.Possibilities.Remove(third.Possibilities);
        if (fourth.Possibilities.Count == 0) return;
        links[cursor++] = fourth.Possibilities;
        possibilityCount += fourth.Possibilities.Count;
        cellCount += fourth.Number;

        var fifth = CrossRowPossibilities(strategyManager, cells[2]);
        if (!fifth.Possibilities.PeekAll(fourth.Possibilities)) return;
        fifth.Possibilities.Remove(fourth.Possibilities);
        if (fifth.Possibilities.Count == 0) return;
        links[cursor++] = fifth.Possibilities;
        possibilityCount += fifth.Possibilities.Count;
        cellCount += fifth.Number;

        var sixth = CrossRowPossibilities(strategyManager, cells[3]);
        if (!sixth.Possibilities.PeekAll(fifth.Possibilities)) return;
        sixth.Possibilities.Remove(fifth.Possibilities);
        if (sixth.Possibilities.Count == 0) return;
        links[cursor++] = sixth.Possibilities;
        possibilityCount += sixth.Possibilities.Count;
        cellCount += sixth.Number;

        var seventh = CrossColPossibilities(strategyManager, cells[3]);
        if (!seventh.Possibilities.PeekAll(sixth.Possibilities)) return;
        seventh.Possibilities.Remove(sixth.Possibilities);
        if (seventh.Possibilities.Count == 0) return;
        links[cursor++] = seventh.Possibilities;
        possibilityCount += seventh.Possibilities.Count;
        cellCount += seventh.Number;

        var eighth = CrossColPossibilities(strategyManager, cells[0]);
        if (!eighth.Possibilities.PeekAll(seventh.Possibilities)) return;
        eighth.Possibilities.Remove(seventh.Possibilities);
        if (eighth.Possibilities.Count == 0) return;
        links[cursor++] = eighth.Possibilities;
        possibilityCount += eighth.Possibilities.Count;
        cellCount += eighth.Number;

        var first = CrossRowPossibilities(strategyManager, cells[0]);
        if (!first.Possibilities.PeekAll(eighth.Possibilities)) return;
        first.Possibilities.Remove(eighth.Possibilities);
        if (fifth.Possibilities.Count == 0) return;
        links[cursor] = first.Possibilities;
        possibilityCount += first.Possibilities.Count;
        cellCount += first.Number;

        if (!start.Equals(first.Possibilities) || possibilityCount > cellCount) return;

        ProcessPattern(strategyManager, cells, links);
    }

    private void ProcessPattern(IStrategyManager strategyManager, Cell[] cells, IPossibilities[] links)
    {
        var miniCol1 = cells[0].Col / 3;
        var miniCol2 = cells[1].Col / 3;
        
        for (int col = 0; col < 9; col++)
        {
            var miniCol = col / 3;
            if(miniCol == miniCol1 || miniCol == miniCol2) continue;

            foreach (var possibility in links[7])
            {
                strategyManager.ChangeBuffer.AddPossibilityToRemove(possibility, cells[0].Row, col);
            }
            
            foreach (var possibility in links[3])
            {
                strategyManager.ChangeBuffer.AddPossibilityToRemove(possibility, cells[2].Row, col);
            }
        }

        for (int gridRow = 0; gridRow < 3; gridRow++)
        {
            for (int gridCol = 0; gridCol < 3; gridCol++)
            {
                int row = cells[1].Row / 3 * 3 + gridRow;
                int col = cells[1].Col / 3 * 3 + gridCol;

                if (row != cells[1].Row && col != cells[1].Col)
                {
                    foreach (var possibility in links[0])
                    {
                        strategyManager.ChangeBuffer.AddPossibilityToRemove(possibility, row, col);
                    }
                }
                
                row = cells[2].Row / 3 * 3 + gridRow;
                col = cells[2].Col / 3 * 3 + gridCol;

                if (row != cells[2].Row && col != cells[2].Col)
                {
                    foreach (var possibility in links[2])
                    {
                        strategyManager.ChangeBuffer.AddPossibilityToRemove(possibility, row, col);
                    }
                }
                
                row = cells[3].Row / 3 * 3 + gridRow;
                col = cells[3].Col / 3 * 3 + gridCol;

                if (row != cells[3].Row && col != cells[3].Col)
                {
                    foreach (var possibility in links[4])
                    {
                        strategyManager.ChangeBuffer.AddPossibilityToRemove(possibility, row, col);
                    }
                }
                
                row = cells[0].Row / 3 * 3 + gridRow;
                col = cells[0].Col / 3 * 3 + gridCol;

                if (row != cells[0].Row && col != cells[0].Col)
                {
                    foreach (var possibility in links[6])
                    {
                        strategyManager.ChangeBuffer.AddPossibilityToRemove(possibility, row, col);
                    }
                }
            }
        }

        var miniRow1 = cells[1].Row / 3;
        var miniRow2 = cells[2].Row / 3;

        for (int row = 0; row < 9; row++)
        {
            var miniRow = row / 3;
            if (miniRow == miniRow1 || miniRow == miniRow2) continue;
            
            foreach (var possibility in links[1])
            {
                strategyManager.ChangeBuffer.AddPossibilityToRemove(possibility, row, cells[1].Col);
            }
            
            foreach (var possibility in links[5])
            {
                strategyManager.ChangeBuffer.AddPossibilityToRemove(possibility, row, cells[3].Col);
            }
        }
        
        if (strategyManager.ChangeBuffer.NotEmpty())
            strategyManager.ChangeBuffer.Push(this, new SKLoopsReportBuilder(cells, links));
    }

    private bool IsCellValid(IStrategyManager strategyManager, int row, int col)
    {
        int startCol = col / 3 * 3;
        int startRow = row / 3 * 3;

        int countRow = 0;
        int countCol = 0;
        
        for (int gridCol = 0; gridCol < 3; gridCol++)
        {
            int crossCol = startCol + gridCol;
            if (crossCol == col) continue;

            if (strategyManager.Sudoku[row, crossCol] != 0) countRow++;
        }
        
        for (int gridRow = 0; gridRow < 3; gridRow++)
        {
            int crossRow = startRow + gridRow;
            if (crossRow == row) continue;

            if (strategyManager.Sudoku[crossRow, col] != 0) countCol++;
        }

        return countRow < 2 & countCol < 2;
    }

    private PossibilitiesAndNumber CrossRowPossibilities(IStrategyManager strategyManager, Cell cell)
    {
        int startCol = cell.Col / 3 * 3;
        IPossibilities result = IPossibilities.NewEmpty();
        int count = 0;

        for (int gridCol = 0; gridCol < 3; gridCol++)
        {
            int crossCol = startCol + gridCol;
            if (crossCol == cell.Col) continue;

            var poss = strategyManager.PossibilitiesAt(cell.Row, crossCol);
            if (poss.Count > 0)
            {
                result.Add(poss);
                count++;
            }
        }

        return new PossibilitiesAndNumber(result, count);
    }

    private PossibilitiesAndNumber CrossColPossibilities(IStrategyManager strategyManager, Cell cell)
    {
        int startRow = cell.Row / 3 * 3;
        IPossibilities result = IPossibilities.NewEmpty();
        int count = 0;

        for (int gridRow = 0; gridRow < 3; gridRow++)
        {
            int crossRow = startRow + gridRow;
            if (crossRow == cell.Row) continue;

            var poss = strategyManager.PossibilitiesAt(crossRow, cell.Col);
            if (poss.Count > 0)
            {
                result.Add(poss);
                count++;
            }
        }

        return new PossibilitiesAndNumber(result, count);
    }
}

public class PossibilitiesAndNumber
{
    public PossibilitiesAndNumber(IPossibilities possibilities, int number)
    {
        Possibilities = possibilities;
        Number = number;
    }

    public IPossibilities Possibilities { get; }
    public int Number { get; }
}

public class SKLoopsReportBuilder : IChangeReportBuilder
{
    private readonly Cell[] _cells;
    private readonly IPossibilities[] _links;

    public SKLoopsReportBuilder(Cell[] cells, IPossibilities[] links)
    {
        _cells = cells;
        _links = links;
    }

    public ChangeReport Build(List<SolverChange> changes, IPossibilitiesHolder snapshot)
    {
        List<CellPossibility> on = new();
        List<CellPossibility> off = new();

        foreach (var cell in CrossRow(snapshot, _cells[1]))
        {
            foreach (var possibility in _links[7])
            {
                if(snapshot.PossibilitiesAt(cell).Peek(possibility))
                    on.Add(new CellPossibility(cell, possibility));
            }
            
            foreach (var possibility in _links[0])
            {
                if(snapshot.PossibilitiesAt(cell).Peek(possibility))
                    off.Add(new CellPossibility(cell, possibility));
            }
        }
        
        foreach (var cell in CrossCol(snapshot, _cells[1]))
        {
            foreach (var possibility in _links[0])
            {
                if(snapshot.PossibilitiesAt(cell).Peek(possibility))
                    on.Add(new CellPossibility(cell, possibility));
            }
            
            foreach (var possibility in _links[1])
            {
                if(snapshot.PossibilitiesAt(cell).Peek(possibility))
                    off.Add(new CellPossibility(cell, possibility));
            }
        }
        
        foreach (var cell in CrossCol(snapshot, _cells[2]))
        {
            foreach (var possibility in _links[1])
            {
                if(snapshot.PossibilitiesAt(cell).Peek(possibility))
                    on.Add(new CellPossibility(cell, possibility));
            }
            
            foreach (var possibility in _links[2])
            {
                if(snapshot.PossibilitiesAt(cell).Peek(possibility))
                    off.Add(new CellPossibility(cell, possibility));
            }
        }
        
        foreach (var cell in CrossRow(snapshot, _cells[2]))
        {
            foreach (var possibility in _links[2])
            {
                if(snapshot.PossibilitiesAt(cell).Peek(possibility))
                    on.Add(new CellPossibility(cell, possibility));
            }
            
            foreach (var possibility in _links[3])
            {
                if(snapshot.PossibilitiesAt(cell).Peek(possibility))
                    off.Add(new CellPossibility(cell, possibility));
            }
        }
        
        foreach (var cell in CrossRow(snapshot, _cells[3]))
        {
            foreach (var possibility in _links[3])
            {
                if(snapshot.PossibilitiesAt(cell).Peek(possibility))
                    on.Add(new CellPossibility(cell, possibility));
            }
            
            foreach (var possibility in _links[4])
            {
                if(snapshot.PossibilitiesAt(cell).Peek(possibility))
                    off.Add(new CellPossibility(cell, possibility));
            }
        }
        
        foreach (var cell in CrossCol(snapshot, _cells[3]))
        {
            foreach (var possibility in _links[4])
            {
                if(snapshot.PossibilitiesAt(cell).Peek(possibility))
                    on.Add(new CellPossibility(cell, possibility));
            }
            
            foreach (var possibility in _links[5])
            {
                if(snapshot.PossibilitiesAt(cell).Peek(possibility))
                    off.Add(new CellPossibility(cell, possibility));
            }
        }
        
        foreach (var cell in CrossCol(snapshot, _cells[0]))
        {
            foreach (var possibility in _links[5])
            {
                if(snapshot.PossibilitiesAt(cell).Peek(possibility))
                    on.Add(new CellPossibility(cell, possibility));
            }
            
            foreach (var possibility in _links[6])
            {
                if(snapshot.PossibilitiesAt(cell).Peek(possibility))
                    off.Add(new CellPossibility(cell, possibility));
            }
        }
        
        foreach (var cell in CrossRow(snapshot, _cells[0]))
        {
            foreach (var possibility in _links[6])
            {
                if(snapshot.PossibilitiesAt(cell).Peek(possibility))
                    on.Add(new CellPossibility(cell, possibility));
            }
            
            foreach (var possibility in _links[7])
            {
                if(snapshot.PossibilitiesAt(cell).Peek(possibility))
                    off.Add(new CellPossibility(cell, possibility));
            }
        }
        
        return new ChangeReport(IChangeReportBuilder.ChangesToString(changes), "", lighter =>
        {
            foreach (var cell in _cells)
            {
                lighter.HighlightCell(cell, ChangeColoration.CauseOffThree);
            }

            foreach (var cell in on)
            {
                lighter.HighlightPossibility(cell, ChangeColoration.CauseOnOne);
            }

            foreach (var cell in off)
            {
                lighter.HighlightPossibility(cell, ChangeColoration.CauseOffTwo);
            }

            IChangeReportBuilder.HighlightChanges(lighter, changes);
        });
    }
    
    private IEnumerable<Cell> CrossRow(IPossibilitiesHolder snapshot, Cell cell)
    {
        int startCol = cell.Col / 3 * 3;

        for (int gridCol = 0; gridCol < 3; gridCol++)
        {
            int crossCol = startCol + gridCol;
            if (crossCol == cell.Col) continue;

            var current = new Cell(cell.Row, crossCol);
            if (snapshot.PossibilitiesAt(current).Count > 0) yield return current;
        }
    }

    private IEnumerable<Cell> CrossCol(IPossibilitiesHolder snapshot, Cell cell)
    {
        int startRow = cell.Row / 3 * 3;

        for (int gridRow = 0; gridRow < 3; gridRow++)
        {
            int crossRow = startRow + gridRow;
            if (crossRow == cell.Row) continue;

            var current = new Cell(crossRow, cell.Col);
            if (snapshot.PossibilitiesAt(current).Count > 0) yield return current;
        }
    }
}