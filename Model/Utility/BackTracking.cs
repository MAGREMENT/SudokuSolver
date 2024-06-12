﻿using System.Collections.Generic;
using Model.Sudokus;
using Model.Sudokus.Solver.Position;
using Model.Tectonics;
using Model.Utility.BitSets;

namespace Model.Utility;

public static class BackTracking
{
    public static IReadOnlyList<Sudoku> Fill(Sudoku start, IPossibilitiesGiver giver, int stopAt)
    {
        var result = new SudokuBackTrackingListResult();
        Start(result, start, giver, stopAt);
        return result;
    }

    public static int Count(Sudoku start, IPossibilitiesGiver giver, int stopAt)
    {
        var result = new SudokuBackTrackingCountResult();
        Start(result, start, giver, stopAt);
        return result.Count;
    }

    //TODO count
    public static IReadOnlyList<ITectonic> Fill(ITectonic start, IPossibilitiesGiver giver, int stopAt)
    {
        var result = new List<ITectonic>();
        Start(result, start, giver, stopAt);
        return result;
    }

    private static void Start(ICollection<ITectonic> result, ITectonic start, IPossibilitiesGiver giver, int stopAt)
    {
        Dictionary<IZone, ReadOnlyBitSet8> zones = new();
        var neighbors = new[]
        {
            new TectonicBitmap(start.RowCount, start.ColumnCount),
            new TectonicBitmap(start.RowCount, start.ColumnCount),
            new TectonicBitmap(start.RowCount, start.ColumnCount),
            new TectonicBitmap(start.RowCount, start.ColumnCount),
            new TectonicBitmap(start.RowCount, start.ColumnCount)
        };

        for (int row = 0; row < start.RowCount; row++)
        {
            for (int col = 0; col < start.ColumnCount; col++)
            {
                var zone = start.GetZone(row, col);
                var bitSet = zones.TryGetValue(zone, out var bs) ? bs : new ReadOnlyBitSet8();
                var number = start[row, col];
                if (number != 0)
                {
                    neighbors[number - 1].Add(row, col);
                    bitSet += number;
                }
                
                zones[zone] = bitSet;
            }
        }

        Search(result, start, giver, zones, neighbors, 0, stopAt);
    }

    private static bool Search(ICollection<ITectonic> result, ITectonic current, IPossibilitiesGiver giver,
        IDictionary<IZone, ReadOnlyBitSet8> zones, TectonicBitmap[] neighbors, int position, int stopAt)
    {
        var full = current.RowCount * current.ColumnCount;
        for (; position < full; position++)
        {
            var row = position / current.ColumnCount;
            var col = position % current.ColumnCount;
            
            if (current[row, col] != 0) continue;

            var zone = current.GetZone(row, col);
            var bitSet = zones.TryGetValue(zone, out var bs) ? bs : new ReadOnlyBitSet8();
            foreach (var possibility in giver.EnumeratePossibilitiesAt(row, col))
            {
                var n = neighbors[possibility - 1];
                if(bitSet.Contains(possibility) || n.HasNeighbor(row, col)) continue;

                current[row, col] = possibility;
                //TODO non readonly bit set ?
                zones[zone] = bitSet + possibility;
                n.Add(row, col);

                if (Search(result, current, giver, zones, neighbors, position + 1, stopAt))
                {
                    current[row, col] = 0;
                    return true;
                }
                
                current[row, col] = 0;
                zones[zone] = bitSet;
                n.Remove(row, col);
            }

            return false;
        }
        
        result.Add(current.Copy());
        return result.Count >= stopAt;
    }
    
    private static void Start(ISudokuBackTrackingResult result, Sudoku start, IPossibilitiesGiver giver, int stopAt)
    {
        var positions = new GridPositions[]
        {
            new(), new(), new(), new(), new(), new(), new(), new(), new()
        };

        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                var n = start[row, col];
                if (n == 0) continue;

                positions[n - 1].Add(row, col);
            }
        }
        
        Search(result, start, giver, positions, 0, stopAt);
    }


    private static bool Search(ISudokuBackTrackingResult result, Sudoku current, IPossibilitiesGiver giver,
        IReadOnlyList<GridPositions> positions, int position, int stopAt)
    {
        for (; position < 81; position++)
        {
            var row = position / 9;
            var col = position % 9;

            if (current[row, col] != 0) continue;
            
            foreach (var possibility in giver.EnumeratePossibilitiesAt(row, col))
            {
                var pos = positions[possibility - 1];
                if (pos.IsRowNotEmpty(row) || pos.IsColumnNotEmpty(col) || pos.IsMiniGridNotEmpty(row / 3, col / 3)) continue;

                current[row, col] = possibility;
                pos.Add(row, col);

                if (Search(result, current, giver, positions, position + 1, stopAt))
                {
                    current[row, col] = 0;
                    return true;
                }

                current[row, col] = 0;
                pos.Remove(row, col);
            }

            return false;
        }
        
        result.AddNewResult(current.Copy());
        return result.Count >= stopAt;
    }
}

public interface IPossibilitiesGiver
{
    IEnumerable<int> EnumeratePossibilitiesAt(int row, int col);
}

public class TectonicPossibilitiesGiver : IPossibilitiesGiver
{
    private readonly ITectonic _tectonic;

    public TectonicPossibilitiesGiver(ITectonic tectonic)
    {
        _tectonic = tectonic;
    }

    public IEnumerable<int> EnumeratePossibilitiesAt(int row, int col)
    {
        var count = _tectonic.GetZone(row, col).Count;
        for (int i = 1; i <= count; i++)
        {
            yield return i;
        }
    }
}

public interface ISudokuBackTrackingResult
{
    void AddNewResult(Sudoku sudoku);
    int Count { get; }
}

public class SudokuBackTrackingListResult : List<Sudoku>, ISudokuBackTrackingResult
{
    public void AddNewResult(Sudoku sudoku)
    {
        Add(sudoku.Copy());
    }
}

public class SudokuBackTrackingCountResult : ISudokuBackTrackingResult
{
    public void AddNewResult(Sudoku sudoku)
    {
        Count++;
    }

    public int Count { get; private set; }
}

