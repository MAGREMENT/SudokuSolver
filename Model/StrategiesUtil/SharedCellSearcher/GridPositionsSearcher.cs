﻿using System.Collections.Generic;
using Model.Positions;
using Model.Solver;

namespace Model.StrategiesUtil.SharedCellSearcher;

public class GridPositionsSearcher : ISharedSeenCellSearcher
{
    public IEnumerable<Coordinate> SharedSeenCells(int row1, int col1, int row2, int col2)
    {
        var one = new GridPositions();
        var two = new GridPositions();

        one.FillRow(row1);
        one.FillColumn(col1);
        one.FillMiniGrid(row1 / 3, col1 / 3);

        two.FillRow(row2);
        two.FillColumn(col2);
        two.FillMiniGrid(row2 / 3, col2 / 3);

        var and = one.And(two);
        and.Remove(row1, col1);
        and.Remove(row2, col2);

        return and;
    }

    public IEnumerable<Coordinate> SharedSeenEmptyCells(IStrategyManager strategyManager, int row1, int col1, int row2, int col2)
    {
        return ISharedSeenCellSearcher.DefaultSharedSeenEmptyCells(this, strategyManager, row1, col1, row2, col2);
    }
}