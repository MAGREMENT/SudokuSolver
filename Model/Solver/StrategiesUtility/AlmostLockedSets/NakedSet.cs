﻿using System.Collections.Generic;
using System.Linq;
using Global;
using Model.Solver.Possibility;
using Model.Solver.StrategiesUtility.Graphs;

namespace Model.Solver.StrategiesUtility.AlmostLockedSets;

public class NakedSet : ILinkGraphElement
{
    private readonly CellPossibilities[] _cellPossibilities;
    
    public int Rank => 3;

    public NakedSet(CellPossibilities[] cellPossibilities)
    {
        _cellPossibilities = cellPossibilities;
    }

    
    public CellPossibilities[] EveryCellPossibilities()
    {
        return _cellPossibilities;
    }

    public Cell[] EveryCell()
    {
        Cell[] result = new Cell[_cellPossibilities.Length];
        for (int i = 0; i < _cellPossibilities.Length; i++)
        {
            result[i] = _cellPossibilities[i].Cell;
        }

        return result;
    }

    public Possibilities EveryPossibilities()
    {
        Possibilities result = Possibilities.NewEmpty();

        foreach (var cp in _cellPossibilities)
        {
            result = result.Or(cp.Possibilities);
        }

        return result;
    }

    public CellPossibility[] EveryCellPossibility()
    {
        List<CellPossibility> result = new();
        foreach (var cp in _cellPossibilities)
        {
            foreach (var p in cp.Possibilities)
            {
                result.Add(new CellPossibility(cp.Cell, p));
            }
        }

        return result.ToArray();
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is not NakedSet np) return false;
        foreach (CellPossibilities cp in _cellPossibilities)
        {
            if (!np._cellPossibilities.Contains(cp)) return false;
        }

        return true;
    }

    public override int GetHashCode()
    {
        var hashCode = 0;
        foreach (var coord in _cellPossibilities)
        {
            hashCode ^= coord.GetHashCode();
        }

        return hashCode;
    }

    public override string ToString()
    {
        var result = $"{EveryPossibilities().ToSlimString()}";
        foreach (var coord in _cellPossibilities)
        {
            result += $"{coord.Cell}, ";
        }

        return result[..^2];
    }
}