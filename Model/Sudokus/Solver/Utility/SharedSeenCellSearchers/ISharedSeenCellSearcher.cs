﻿using System.Collections.Generic;
using Model.Utility;

namespace Model.Sudokus.Solver.Utility.SharedSeenCellSearchers;

/// <summary>
/// Order of fastest implementation :
/// 1 -> InCommonFindSearcher
/// 2 -> SeenCellCompareSearcher
/// 3 -> GridPositionsCompareSearcher
/// 4 -> FullGridCheckSearcher
/// </summary>
public interface ISharedSeenCellSearcher
{
    IEnumerable<Cell> SharedSeenCells(int row1, int col1, int row2, int col2);

    IEnumerable<Cell> SharedSeenEmptyCells(ISudokuSolverData solverData, int row1, int col1, int row2,
        int col2);

    IEnumerable<CellPossibility> SharedSeenPossibilities(int row1, int col1, int pos1, int row2, int col2, int pos2);

    IEnumerable<CellPossibility> SharedSeenExistingPossibilities(ISudokuSolverData solverData, int row1, int col1,
        int pos1, int row2, int col2, int pos2);

    static IEnumerable<Cell> DefaultSharedSeenEmptyCells(ISharedSeenCellSearcher searcher,
        ISudokuSolverData solverData, int row1, int col1, int row2, int col2)
    {
        foreach (var coord in searcher.SharedSeenCells(row1, col1, row2, col2))
        {
            if (solverData.Sudoku[coord.Row, coord.Column] == 0) yield return coord;
        }
    }

}