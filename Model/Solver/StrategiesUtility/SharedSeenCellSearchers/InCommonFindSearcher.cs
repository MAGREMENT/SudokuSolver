﻿using System.Collections.Generic;
using Global;

namespace Model.Solver.StrategiesUtility.SharedSeenCellSearchers;

public class InCommonFindSearcher : ISharedSeenCellSearcher
{
    public IEnumerable<Cell> SharedSeenCells(int row1, int col1, int row2, int col2)
    {
        int miniRow1 = row1 / 3;
        int miniCol1 = col1 / 3;
        int miniRow2 = row2 / 3;
        int miniCol2 = col2 / 3;

        if (miniRow1 == miniRow2 && miniCol1 == miniCol2)
        {
            var startRow = miniRow1 * 3;
            var startCol = miniCol1 * 3;

            for (int gridRow = 0; gridRow < 3; gridRow++)
            {
                for (int gridCol = 0; gridCol < 3; gridCol++)
                {
                    var row = startRow + gridRow;
                    var col = startCol + gridCol;
                    
                    if((row == row1 && col == col1) || (row == row2 && col == col2)) continue;
                    yield return new Cell(row, col);
                }
            }

            if (row1 == row2)
            {
                for (int miniCol = 0; miniCol < 3; miniCol++)
                {
                    if(miniCol == miniCol1) continue;

                    var sc = miniCol * 3;
                    for (int gridCol = 0; gridCol < 3; gridCol++)
                    {
                        yield return new Cell(row1, sc + gridCol);
                    }
                }
            }

            if (col1 == col2)
            {
                for (int miniRow = 0; miniRow < 3; miniRow++)
                {
                    if(miniRow == miniRow1) continue;

                    var sr = miniRow * 3;
                    for (int gridRow = 0; gridRow < 3; gridRow++)
                    {
                        yield return new Cell(sr + gridRow, col1);
                    }
                }
            }
            
            yield break;
        }

        if (row1 == row2)
        {
            for (int col = 0; col < 9; col++)
            {
                if(col == col1 || col == col2) continue;

                yield return new Cell(row1, col);
            }
            
            yield break;
        }

        if (col1 == col2)
        {
            for (int row = 0; row < 9; row++)
            {
                if(row == row1 || row == row2) continue;

                yield return new Cell(row, col1);
            }

            yield break;
        }

        if (miniRow1 == miniRow2)
        {
            var start1 = miniCol1 * 3;
            var start2 = miniCol2 * 3;

            for (int gridCol = 0; gridCol < 3; gridCol++)
            {
                yield return new Cell(row2, start1 + gridCol);
                yield return new Cell(row1, start2 + gridCol);
            }

            yield break;
        }

        if (miniCol1 == miniCol2)
        {
            var start1 = miniRow1 * 3;
            var start2 = miniRow2 * 3;

            for (int gridRow = 0; gridRow < 3; gridRow++)
            {
                yield return new Cell(start1 + gridRow, col2);
                yield return new Cell(start2 + gridRow, col1);
            }

            yield break;
        }

        yield return new Cell(row1, col2);
        yield return new Cell(row2, col1);
    }

    public IEnumerable<Cell> SharedSeenEmptyCells(IStrategyManager strategyManager, int row1, int col1, int row2, int col2)
    {
        int miniRow1 = row1 / 3;
        int miniCol1 = col1 / 3;
        int miniRow2 = row2 / 3;
        int miniCol2 = col2 / 3;

        if (miniRow1 == miniRow2 && miniCol1 == miniCol2)
        {
            var startRow = miniRow1 * 3;
            var startCol = miniCol1 * 3;

            for (int gridRow = 0; gridRow < 3; gridRow++)
            {
                for (int gridCol = 0; gridCol < 3; gridCol++)
                {
                    var row = startRow + gridRow;
                    var col = startCol + gridCol;
                    
                    if((row == row1 && col == col1) || (row == row2 && col == col2)) continue;
                    if(strategyManager.Sudoku[row, col] == 0) yield return new Cell(row, col);
                }
            }

            if (row1 == row2)
            {
                for (int miniCol = 0; miniCol < 3; miniCol++)
                {
                    if(miniCol == miniCol1) continue;

                    var sc = miniCol * 3;
                    for (int gridCol = 0; gridCol < 3; gridCol++)
                    {
                        if(strategyManager.Sudoku[row1, sc + gridCol] == 0) yield return new Cell(row1, sc + gridCol);
                    }
                }
            }

            if (col1 == col2)
            {
                for (int miniRow = 0; miniRow < 3; miniRow++)
                {
                    if(miniRow == miniRow1) continue;

                    var sr = miniRow * 3;
                    for (int gridRow = 0; gridRow < 3; gridRow++)
                    {
                        if(strategyManager.Sudoku[sr + gridRow, col1] == 0) yield return new Cell(sr + gridRow, col1);
                    }
                }
            }
            
            yield break;
        }

        if (row1 == row2)
        {
            for (int col = 0; col < 9; col++)
            {
                if(col == col1 || col == col2) continue;

                if(strategyManager.Sudoku[row1, col] == 0) yield return new Cell(row1, col);
            }
            
            yield break;
        }

        if (col1 == col2)
        {
            for (int row = 0; row < 9; row++)
            {
                if(row == row1 || row == row2) continue;

                if(strategyManager.Sudoku[row, col1] == 0) yield return new Cell(row, col1);
            }

            yield break;
        }

        if (miniRow1 == miniRow2)
        {
            var start1 = miniCol1 * 3;
            var start2 = miniCol2 * 3;

            for (int gridCol = 0; gridCol < 3; gridCol++)
            {
                if(strategyManager.Sudoku[row2, start1 + gridCol] == 0) yield return new Cell(row2, start1 + gridCol);
                if(strategyManager.Sudoku[row1, start2 + gridCol] == 0) yield return new Cell(row1, start2 + gridCol);
            }

            yield break;
        }

        if (miniCol1 == miniCol2)
        {
            var start1 = miniRow1 * 3;
            var start2 = miniRow2 * 3;

            for (int gridRow = 0; gridRow < 3; gridRow++)
            {
                if(strategyManager.Sudoku[start1 + gridRow, col2] == 0) yield return new Cell(start1 + gridRow, col2);
                if(strategyManager.Sudoku[start2 + gridRow, col1] == 0) yield return new Cell(start2 + gridRow, col1);
            }

            yield break;
        }

        if(strategyManager.Sudoku[row1, col2] == 0) yield return new Cell(row1, col2);
        if(strategyManager.Sudoku[row2, col1] == 0) yield return new Cell(row2, col1);
    }

    public IEnumerable<CellPossibility> SharedSeenPossibilities(int row1, int col1, int pos1, int row2, int col2, int pos2)
    {
        if (pos1 == pos2)
        {
            foreach (var cell in SharedSeenCells(row1, col1, row2, col2))
            {
                yield return new CellPossibility(cell, pos1);
            }
        }
        else
        {
            //TODO
        }
    }

    public IEnumerable<CellPossibility> SharedSeenExistingPossibilities(IStrategyManager strategyManager, int row1, int col1, int pos1, int row2,
        int col2, int pos2)
    {
        if (pos1 == pos2)
        {
            foreach (var cell in SharedSeenCells(row1, col1, row2, col2))
            {
                if(strategyManager.PossibilitiesAt(cell).Peek(pos1)) yield return new CellPossibility(cell, pos1);
            }
        }
        else
        {
            if (row1 == row2 && col1 == col2)
            {
                foreach (var p in strategyManager.PossibilitiesAt(row1, col1))
                {
                    if (p == pos1 || p == pos2) continue;
                    yield return new CellPossibility(row1, col1, p);
                }
            }
            else if(Cells.ShareAUnit(row1, col1, row2, col2))
            {
                if(strategyManager.PossibilitiesAt(row1, col1).Peek(pos2)) yield return new CellPossibility(row1, col1, pos2);
                if(strategyManager.PossibilitiesAt(row2, col2).Peek(pos1)) yield return new CellPossibility(row2, col2, pos1);
            }
        }
    }
}