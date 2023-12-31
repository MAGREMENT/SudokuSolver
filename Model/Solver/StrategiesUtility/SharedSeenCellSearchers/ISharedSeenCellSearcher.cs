﻿using System.Collections.Generic;
using Global;

namespace Model.Solver.StrategiesUtility.SharedSeenCellSearchers;

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

    IEnumerable<Cell> SharedSeenEmptyCells(IStrategyManager strategyManager, int row1, int col1, int row2,
        int col2);

    IEnumerable<CellPossibility> SharedSeenPossibilities(int row1, int col1, int pos1, int row2, int col2, int pos2);

    IEnumerable<CellPossibility> SharedSeenExistingPossibilities(IStrategyManager strategyManager, int row1, int col1,
        int pos1, int row2, int col2, int pos2);

    static IEnumerable<Cell> DefaultSharedSeenEmptyCells(ISharedSeenCellSearcher searcher,
        IStrategyManager strategyManager, int row1, int col1, int row2, int col2)
    {
        foreach (var coord in searcher.SharedSeenCells(row1, col1, row2, col2))
        {
            if (strategyManager.Sudoku[coord.Row, coord.Column] == 0) yield return coord;
        }
    }

}