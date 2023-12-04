﻿using System.Collections.Generic;
using Global;

namespace Model.Solver.Position;

public interface IReadOnlyGridPositions : IEnumerable<Cell>
{
    bool Peek(Cell cell);
    int RowCount(int row);
    int ColumnCount(int col);
    int MiniGridCount(int miniRow, int miniCol);
    GridPositions Copy();
    GridPositions And(IReadOnlyGridPositions pos);
    GridPositions Or(IReadOnlyGridPositions pos);
    GridPositions Difference(IReadOnlyGridPositions with);

    static GridPositions DefaultAnd(IReadOnlyGridPositions one, IReadOnlyGridPositions two)
    {
        var result = new GridPositions();
        foreach (var pos in one)
        {
            if(two.Peek(pos)) result.Remove(pos);
        }

        return result;
    }
    
    static GridPositions DefaultDifference(IReadOnlyGridPositions from, IReadOnlyGridPositions with)
    {
        var result = from.Copy();
        foreach (var pos in with)
        {
            result.Remove(pos);
        }

        return result;
    }
}