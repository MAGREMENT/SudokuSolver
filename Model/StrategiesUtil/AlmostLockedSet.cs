﻿using System;
using System.Collections.Generic;
using System.Linq;
using Model.Possibilities;

namespace Model.StrategiesUtil;

public class AlmostLockedSet : ILinkGraphElement
{
    private readonly Coordinate[] _coords;
    public IPossibilities Possibilities { get; }

    public AlmostLockedSet(Coordinate[] coords, IPossibilities poss)
    {
        _coords = coords;
        Possibilities = poss;
    }

    public AlmostLockedSet(Coordinate coord, IPossibilities poss)
    {
        _coords = new[] { coord };
        Possibilities = poss;
    }

    public bool Contains(Coordinate coord)
    {
        return _coords.Contains(coord);
    }


    public static IEnumerable<AlmostLockedSet> SearchForSingleCellAls(ISolverView solverView, List<Coordinate> coords)
    {
        foreach (var coord in coords)
        {
            IPossibilities current = solverView.Possibilities[coord.Row, coord.Col];
            if (current.Count == 2) yield return new AlmostLockedSet(coord, current);
        }
    }

    public static IEnumerable<AlmostLockedSet> SearchForMultipleCellsAls(ISolverView view, List<Coordinate> coords, int count)
    {
        List<AlmostLockedSet> result = new List<AlmostLockedSet>();
        for (int i = 0; i < coords.Count; i++)
        {
            SearchForMultipleCellsAls(view, result, view.Possibilities[coords[i].Row, coords[i].Col], coords,
                i, count - 1, count + 1, new List<Coordinate> {coords[i]});
        }

        return result;
    }
    
    private static void SearchForMultipleCellsAls(ISolverView view, List<AlmostLockedSet> result,
        IPossibilities current, List<Coordinate> coords, int start, int count, int type, List<Coordinate> visited)
    {
        for (int i = start + 1; i < coords.Count; i++)
        {
            if (!ShareAUnitWithAll(coords[i], visited) ||
                !ShareAtLeastOne(view.Possibilities[coords[i].Row, coords[i].Col], current)) continue;
            IPossibilities mashed = current.Mash(view.Possibilities[coords[i].Row, coords[i].Col]);
            var next = new List<Coordinate>(visited) {coords[i]};
            if (count - 1 == 0)
            {
                if(mashed.Count == type) result.Add(new AlmostLockedSet(next.ToArray(), mashed));
            }else if (mashed.Count <= type)
            {
                SearchForMultipleCellsAls(view, result, mashed, coords, i, count - 1, type,
                    next);
            }
        }
    }
    
    private static bool ShareAUnitWithAll(Coordinate current, IEnumerable<Coordinate> coordinates)
    {
        foreach (var coord in coordinates)
        {
            if (!current.ShareAUnit(coord)) return false;
        }

        return true;
    }

    private static bool ShareAtLeastOne(IPossibilities one, IPossibilities two)
    {
        foreach (var possibility in one)
        {
            if (two.Peek(possibility)) return true;
        }

        return false;
    }

    public IEnumerable<Coordinate> SharedSeenCells()
    {
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                Coordinate current = new Coordinate(row, col);

                if (ShareAUnitWithAll(current, _coords)) yield return current;
            }
        }
    }

    public bool ShareAUnit(Coordinate coord)
    {
        foreach (var c in _coords)
        {
            if (!c.ShareAUnit(coord)) return false;
        }

        return true;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not AlmostLockedSet als) return false;
        if (!Possibilities.Equals(als.Possibilities) || _coords.Length != als._coords.Length) return false;
        foreach (var coord in _coords)
        {
            if (!als.Contains(coord)) return false;
        }

        return true;
    }

    public override int GetHashCode()
    {
        int coordHashCode = 0;
        foreach (var coord in _coords)
        {
            coordHashCode ^= coord.GetHashCode();
        }

        return HashCode.Combine(Possibilities.GetHashCode(), coordHashCode);
    }

    public override string ToString()
    {
        var result = $"[ALS : {_coords[0].Row + 1}, {_coords[0].Col + 1} ";
        for (int i = 1; i < _coords.Length; i++)
        {
            result += $"| {_coords[i].Row + 1}, {_coords[i].Col + 1} ";
        }

        result += "=> ";
        foreach (var possibility in Possibilities)
        {
            result += $"{possibility}, ";
        }

        return result[..^2] + "]";
    }
}