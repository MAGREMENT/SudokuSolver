﻿using System.Collections.Generic;
using Model.Solver;

namespace Model.StrategiesUtil.SharedCellSearcher;

/// <summary>
/// Order of fastest implementation :
/// 1 -> InCommonFindSearcher
/// 2 -> SeenCellCompareSearcher
/// 3 -> GridPositionsCompareSearcher
/// 4 -> FullGridCheckSearcher
/// </summary>
public interface ISharedSeenCellSearcher
{
    IEnumerable<Coordinate> SharedSeenCells(int row1, int col1, int row2, int col2);

    IEnumerable<Coordinate> SharedSeenEmptyCells(IStrategyManager strategyManager, int row1, int col1, int row2,
        int col2);

    static IEnumerable<Coordinate> DefaultSharedSeenEmptyCells(ISharedSeenCellSearcher searcher,
        IStrategyManager strategyManager, int row1, int col1, int row2, int col2)
    {
        foreach (var coord in searcher.SharedSeenCells(row1, col1, row2, col2))
        {
            if (strategyManager.Sudoku[coord.Row, coord.Row] == 0) yield return coord;
        }
    }

}