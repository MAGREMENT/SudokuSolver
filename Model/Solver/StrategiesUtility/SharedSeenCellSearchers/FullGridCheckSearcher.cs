﻿using System.Collections.Generic;
using Global;

namespace Model.Solver.StrategiesUtility.SharedSeenCellSearchers;

public class FullGridCheckSearcher : ISharedSeenCellSearcher
{
    public IEnumerable<Cell> SharedSeenCells(int row1, int col1, int row2, int col2)
    {
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                if ((row == row1 && col == col1) || (row == row2 && col == col2)) continue;
                
                if (Cells.ShareAUnit(row, col, row1, col1)
                    && Cells.ShareAUnit(row, col, row2, col2))
                {
                    yield return new Cell(row, col); 
                }
            }
        }
    }

    public IEnumerable<Cell> SharedSeenEmptyCells(IStrategyManager strategyManager, int row1, int col1, int row2, int col2)
    {
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                if (strategyManager.Sudoku[row, col] != 0 ||
                    (row == row1 && col == col1) || (row == row2 && col == col2)) continue;
                
                if (Cells.ShareAUnit(row, col, row1, col1)
                    && Cells.ShareAUnit(row, col, row2, col2))
                {
                    yield return new Cell(row, col);  
                }
            }
        }
    }

    public IEnumerable<CellPossibility> SharedSeenPossibilities(int row1, int col1, int pos1, int row2, int col2, int pos2)
    {
        throw new System.NotImplementedException();
    }

    public IEnumerable<CellPossibility> SharedSeenExistingPossibilities(IStrategyManager strategyManager, int row1, int col1, int pos1, int row2,
        int col2, int pos2)
    {
        throw new System.NotImplementedException();
    }
}