﻿using System;
using System.Collections.Generic;
using Model.Helpers;
using Model.Sudokus.Solver.Utility.Graphs;
using Model.Sudokus.Solver.Utility.SharedSeenCellSearchers;
using Model.Utility;
using Model.Utility.BitSets;

namespace Model.Sudokus.Solver.Utility;

public static class SudokuCellUtility
{
    private static readonly ISharedSeenCellSearcher Searcher = new InCommonFindSearcher();

    public static int SharedRow(IEnumerable<Cell> cells)
    {
        int r = -1;

        foreach (var cell in cells)
        {
            if (r != -2)
            {
                if (r == -1) r = cell.Row;
                else if (r != cell.Row) r = -2;
            }
        }

        return r;
    }

    public static int SharedColumn(IEnumerable<Cell> cells)
    {
        int c = -1;

        foreach (var cell in cells)
        {
            if (c != -2)
            {
                if (c == -1) c = cell.Column;
                else if (c != cell.Column) c = -2;
            }
        }

        return c;
    }

    public static (int, int) SharedLines(IEnumerable<Cell> cells)
    {
        int r = -1;
        int c = -1;
        
        foreach (var cell in cells)
        {
            if (r != -2)
            {
                if (r == -1) r = cell.Row;
                else if (r != cell.Row) r = -2;
            }
            
            if (c != -2)
            {
                if (c == -1) c = cell.Column;
                else if (c != cell.Column) c = -2;
            }
        }

        return (r, c);
    }
    
    public static bool ShareAUnit(int row1, int col1, int row2, int col2)
    {
        return row1 == row2 || col1 == col2 || (row1 / 3 == row2 / 3 && col1 / 3 == col2 / 3);
    }

    public static bool ShareAUnit(Cell one, Cell two)
    {
        return ShareAUnit(one.Row, one.Column, two.Row, two.Column);
    }

    public static bool ShareAUnitWithAll(Cell cell, List<Cell> cells)
    {
        foreach (var c in cells)
        {
            if (!ShareAUnit(cell, c)) return false;
        }

        return true;
    }

    public static List<Cell> SeenCells(Cell cell)
    {
        List<Cell> result = new();
        
        for (int i = 0; i < 9; i++)
        {
            if (i != cell.Row) result.Add(new Cell(i, cell.Column));
            if (i != cell.Column) result.Add(new Cell(cell.Row, i));
        }

        var startRow = cell.Row / 3 * 3;
        var startCol = cell.Column / 3 * 3;
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                var row = startRow + i;
                var col = startCol + i;

                if (row != cell.Row && col != cell.Column) result.Add(new Cell(row, col));
            }
        }

        return result;
    }
    
    public static IEnumerable<Cell> SharedSeenCells(int row1, int col1, int row2, int col2) //TODO "AsList"
    {
        return Searcher.SharedSeenCells(row1, col1, row2, col2);
    }

    public static IEnumerable<Cell> SharedSeenCells(Cell one, Cell two)
    {
        return SharedSeenCells(one.Row, one.Column, two.Row, two.Column);
    }

    public static IEnumerable<Cell> SharedSeenCells(Cell one, Cell two, params Cell[] others)
    {
        foreach (var coord in SharedSeenCells(one, two))
        {
            bool ok = true;
            foreach (var other in others)
            {
                if (!ShareAUnit(other, coord) || other == coord)
                {
                    ok = false;
                    break;
                }
            }

            if (ok) yield return coord;
        }
    }
    
    public static List<Cell> SharedSeenCells(IReadOnlyList<Cell> list)
    {
        if (list.Count == 0) return new List<Cell>();
        if (list.Count == 1) return SeenCells(list[^1]);

        var result = new List<Cell>();
        foreach (var coord in SharedSeenCells(list[0], list[1]))
        {
            bool ok = true;
            for (int i = 2; i < list.Count; i++)
            {
                if (!ShareAUnit(list[i], coord) || list[i] == coord)
                {
                    ok = false;
                    break;
                }
            }

            if (ok) result.Add(coord);
        }

        return result;
    }
    
    public static List<Cell> SeenEmptyCells(ISudokuSolvingState holder, Cell cell)
    {
        List<Cell> result = new();
        
        for (int i = 0; i < 9; i++)
        {
            if (i != cell.Row && holder[i, cell.Column] == 0) result.Add(new Cell(i, cell.Column));
            if (i != cell.Column && holder[cell.Row, i] == 0) result.Add(new Cell(cell.Row, i));
        }

        var startRow = cell.Row / 3 * 3;
        var startCol = cell.Column / 3 * 3;
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                var row = startRow + i;
                var col = startCol + i;

                if (row != cell.Row && col != cell.Column && holder[row, col] == 0) result.Add(new Cell(row, col));
            }
        }

        return result;
    }

    public static IEnumerable<Cell> SharedSeenEmptyCells(IStrategyUser strategyUser, int row1, int col1, int row2, int col2)
    {
        return Searcher.SharedSeenEmptyCells(strategyUser, row1, col1, row2, col2);
    }
    
    public static List<Cell> SharedSeenEmptyCells(IStrategyUser strategyUser, IReadOnlyList<Cell> list)
    {
        if (list.Count == 0) return new List<Cell>();
        if (list.Count == 1) return SeenEmptyCells(strategyUser, list[^1]);

        var result = new List<Cell>();
        foreach (var coord in Searcher.SharedSeenEmptyCells(strategyUser, list[0].Row, list[0].Column,
                     list[1].Row, list[1].Column))
        {
            bool ok = true;
            for (int i = 2; i < list.Count; i++)
            {
                if (!ShareAUnit(list[i], coord) || list[i] == coord)
                {
                    ok = false;
                    break;
                }
            }

            if (ok) result.Add(coord);
        }

        return result;
    }

    public static List<Cell> SharedSeenEmptyCells(IStrategyUser strategyUser, Cell one, Cell two,
        params Cell[] others)
    {
        List<Cell> result = new List<Cell>();
        foreach (var coord in SharedSeenEmptyCells(strategyUser, one.Row, one.Column, two.Row, two.Column))
        {
            bool ok = true;
            foreach (var other in others)
            {
                if (!ShareAUnit(other, coord) || other == coord)
                {
                    ok = false;
                    break;
                }
            }

            if (ok) result.Add(coord);
        }

        return result;
    }
    
    public static bool AreSpreadOverTwoBoxes(int row1, int col1, int row2, int col2)
    {
        return (row1 / 3 != row2 / 3) ^ (col1 / 3 != col2 / 3);
    }
    
    public static bool AreLinked(CellPossibility first, CellPossibility second)
    {
        if (first == second) return false;
        return (first.Row == second.Row && first.Column == second.Column) ||
               (ShareAUnit(first.Row, first.Column, second.Row, second.Column) && first.Possibility == second.Possibility);
    }

    public static List<CellPossibility> SeenExistingPossibilities(IStrategyUser strategyUser, CellPossibility cp)
    {
        var result = new List<CellPossibility>();

        foreach (var poss in strategyUser.PossibilitiesAt(cp.Row, cp.Column).EnumeratePossibilities())
        {
            if (poss == cp.Possibility) continue;
            
            result.Add(new CellPossibility(cp.Row, cp.Column, poss));
        }

        foreach (var col in strategyUser.RowPositionsAt(cp.Row, cp.Possibility))
        {
            if (col == cp.Column) continue;

            result.Add(new CellPossibility(cp.Row, col, cp.Possibility));
        }
        
        foreach (var row in strategyUser.ColumnPositionsAt(cp.Column, cp.Possibility))
        {
            if (row == cp.Row) continue;

            result.Add(new CellPossibility(row, cp.Column, cp.Possibility));
        }
        
        foreach (var pos in strategyUser.MiniGridPositionsAt(cp.Row / 3, cp.Column / 3, cp.Possibility))
        {
            if (pos == cp.ToCell()) continue;

            result.Add(new CellPossibility(pos, cp.Possibility));
        }

        return result;
    }

    public static IEnumerable<CellPossibility> SharedSeenExistingPossibilities(IStrategyUser strategyUser, CellPossibility first,
        CellPossibility second)
    {
        return Searcher.SharedSeenExistingPossibilities(strategyUser, first.Row, first.Column, first.Possibility,
            second.Row, second.Column, second.Possibility);
    }

    public static List<CellPossibility> SharedSeenExistingPossibilities(IStrategyUser strategyUser,
        IReadOnlyList<CellPossibility> list) //TODO USE THIS
    {
        if (list.Count == 0) return new List<CellPossibility>();
        if (list.Count == 1) return SeenExistingPossibilities(strategyUser, list[0]);

        var result = new List<CellPossibility>();
        foreach (var cp in SharedSeenExistingPossibilities(strategyUser, list[0], list[1]))
        {
            bool ok = true;
            for (int i = 2; i < list.Count; i++)
            {
                if (!AreLinked(cp, list[i]) || list[i] == cp)
                {
                    ok = false;
                    break;
                }
            }

            if(ok) result.Add(cp);
        }

        return result;
    }
    
    public static List<CellPossibility> SharedSeenExistingPossibilities(IStrategyUser strategyUser,
        IReadOnlyList<CellPossibility> list, int count)
    {
        if (count > list.Count) return new List<CellPossibility>();
        if (count == 0) return new List<CellPossibility>();
        if (count == 1) return SeenExistingPossibilities(strategyUser, list[0]);

        var result = new List<CellPossibility>();
        foreach (var cp in SharedSeenExistingPossibilities(strategyUser, list[0], list[1]))
        {
            bool ok = true;
            for (int i = 2; i < count ; i++)
            {
                if (!AreLinked(cp, list[i]) || list[i] == cp)
                {
                    ok = false;
                    break;
                }
            }

            if(ok) result.Add(cp);
        }

        return result;
    }

    public static IEnumerable<CellPossibility> DefaultStrongLinks(IStrategyUser strategyUser, CellPossibility cp)
    {
        var poss = strategyUser.PossibilitiesAt(cp.Row, cp.Column);
        if (poss.Count == 2) yield return new CellPossibility(cp.Row, cp.Column, poss.FirstPossibility(cp.Possibility));

        var rPos = strategyUser.RowPositionsAt(cp.Row, cp.Possibility);
        if (rPos.Count == 2) yield return new CellPossibility(cp.Row, rPos.First(cp.Column), cp.Possibility);

        var cPos = strategyUser.ColumnPositionsAt(cp.Column, cp.Possibility);
        if (cPos.Count == 2) yield return new CellPossibility(cPos.First(cp.Row), cp.Column, cp.Possibility);

        var mPos = strategyUser.MiniGridPositionsAt(cp.Row / 3, cp.Column / 3, cp.Possibility);
        if (mPos.Count == 2) yield return new CellPossibility(mPos.First(cp.ToCell()), cp.Possibility);
    }

    public static bool AreStronglyLinked(IStrategyUser strategyUser, CellPossibility cp1, CellPossibility cp2)
    {
        if (cp1.Row == cp2.Row && cp1.Column == cp2.Column)
            return strategyUser.PossibilitiesAt(cp1.Row, cp2.Column).Count == 2;

        if (cp1.Possibility == cp2.Possibility)
        {
            return (cp1.Row == cp2.Row && strategyUser.RowPositionsAt(cp1.Row, cp1.Possibility).Count == 2) ||
                   (cp1.Column == cp2.Column &&
                    strategyUser.ColumnPositionsAt(cp1.Column, cp1.Possibility).Count == 2) ||
                   (cp1.Row / 3 == cp2.Row / 3 && cp1.Column / 3 == cp2.Column / 3 && strategyUser
                       .MiniGridPositionsAt(cp1.Row / 3, cp1.Column / 3, cp1.Possibility).Count == 2);
        }

        return false;
    }
    
    public static IEnumerable<Cell[]> DeadlyPatternRoofs(Cell[] floor)
    {
        if (floor.Length != 2) yield break;
        
        if (floor[0].Row == floor[1].Row)
        {
            if (floor[0].Column / 3 == floor[1].Column / 3)
            {
                var miniRow = floor[0].Row / 3;
                for (int row = 0; row < 9; row++)
                {
                    if (miniRow == row / 3) continue;

                    yield return new[]
                    {
                        new Cell(row, floor[0].Column),
                        new Cell(row, floor[1].Column)
                    };
                }
            }
            else
            {
                var startRow = floor[0].Row / 3 * 3;
                for (int row = startRow; row < startRow + 3; row++)
                {
                    if (row == floor[0].Row) continue;

                    yield return new[]
                    {
                        new Cell(row, floor[0].Column),
                        new Cell(row, floor[1].Column)
                    };
                }
            }
        }
        else if (floor[0].Column == floor[1].Column)
        {
            if (floor[0].Row / 3 == floor[1].Row / 3)
            {
                var miniCol = floor[0].Column / 3;
                for (int col = 0; col < 9; col++)
                {
                    if (miniCol == col / 3) continue;

                    yield return new[]
                    {
                        new Cell(floor[0].Row, col),
                        new Cell(floor[1].Row, col)
                    };
                }
            }
            else
            {
                var startCol = floor[0].Column / 3 * 3;
                for (int col = startCol; col < startCol + 3; col++)
                {
                    if (col == floor[0].Column) continue;

                    yield return new[]
                    {
                        new Cell(floor[0].Row, col), 
                        new Cell(floor[1].Row, col)
                    };
                }
            }
        }
        else
        {
            if (!AreSpreadOverTwoBoxes(floor[0].Row, floor[0].Column, floor[1].Row, floor[1].Column)) yield break;
        
            yield return new[]
            {
                new Cell(floor[0].Row, floor[1].Column),
                new Cell(floor[1].Row, floor[0].Column)
            };  
        }
    }

    public static IEnumerable<(MiniGrid, MiniGrid)> DiagonalMiniGridAssociation(int miniRowExcept, int miniColExcept)
    {
        List<MiniGrid> buffer = new(4);

        for (int miniRow = 0; miniRow < 3; miniRow++)
        {
            if (miniRow == miniRowExcept) continue;
            
            for (int miniCol = 0; miniCol < 3; miniCol++)
            {
                if (miniCol == miniColExcept) continue;

                buffer.Add(new MiniGrid(miniRow, miniCol));
            }
        }

        yield return (buffer[0], buffer[3]);
        yield return (buffer[1], buffer[2]);
    }
    
    public static double Distance(Cell oneCell, int onePoss, Cell twoCell, int twoPoss)
    {
        var oneX = oneCell.Column * 3 + onePoss % 3;
        var oneY = oneCell.Row * 3 + onePoss / 3;

        var twoX = twoCell.Column * 3 + twoPoss % 3;
        var twoY = twoCell.Row * 3 + twoPoss / 3;

        var dx = twoX - oneX;
        var dy = twoY - oneY;

        return Math.Sqrt(dx * dx + dy * dy);
    }
}

public interface ICellPossibility
{
    public int Possibility { get; }
    public int Row { get; }
    public int Column { get; } 
}

public readonly struct CellPossibility : ISudokuElement, ICellPossibility
{
    public int Possibility { get; }
    public int Row { get; }
    public int Column { get; }

    public CellPossibility(int row, int col, int possibility)
    {
        Possibility = possibility;
        Row = row;
        Column = col;
    }

    public CellPossibility(Cell coord, int possibility)
    {
        Possibility = possibility;
        Row = coord.Row;
        Column = coord.Column;
    }
    
    public bool ShareAUnit(CellPossibility coord)
    {
        return SudokuCellUtility.ShareAUnit(Row, Column, coord.Row, coord.Column);
    }
    
    public bool ShareAUnit(Cell coord)
    {
        return SudokuCellUtility.ShareAUnit(Row, Column, coord.Row, coord.Column);
    }

    public IEnumerable<Cell> SharedSeenCells(CellPossibility coord)
    {
        return SudokuCellUtility.SharedSeenCells(Row, Column, coord.Row, coord.Column);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Possibility, Row, Column);
    }

    public override bool Equals(object? obj)
    {
        return (obj is CellPossibility cp && cp == this) ||
               (obj is ICellPossibility icp && icp.Possibility == Possibility && icp.Row == Row && icp.Column == Column);
    }

    public override string ToString()
    {
        return $"{Possibility}r{Row + 1}c{Column + 1}";
    }

    public int DifficultyRank => 1;

    public CellPossibilities[] EveryCellPossibilities()
    {
        return new[] { new CellPossibilities(this) };
    }

    public Cell[] EveryCell()
    {
        return new Cell[] { new(Row, Column) };
    }

    public ReadOnlyBitSet16 EveryPossibilities()
    {
        var result = new ReadOnlyBitSet16();
        result += Possibility;
        return result;
    }

    public CellPossibility[] EveryCellPossibility()
    {
        return new[] { this };
    }

    public bool Contains(Cell cell)
    {
        return cell.Row == Row && cell.Column == Column;
    }

    public bool Contains(CellPossibility cp)
    {
        return cp == this;
    }

    public Cell ToCell()
    {
        return new Cell(Row, Column);
    }

    public static bool operator ==(CellPossibility left, CellPossibility right)
    {
        return left.Possibility == right.Possibility && left.Row == right.Row && left.Column == right.Column;
    }

    public static bool operator !=(CellPossibility left, CellPossibility right)
    {
        return !(left == right);
    }
}

public class CellPossibilities
{
    public Cell Cell { get; }
    public ReadOnlyBitSet16 Possibilities { get; }
    
    public CellPossibilities(Cell cell, ReadOnlyBitSet16 possibilities)
    {
        Cell = cell;
        Possibilities = possibilities;
    }

    public CellPossibilities(Cell cell, int possibility)
    {
        Cell = cell;
        var buffer = new ReadOnlyBitSet16();
        buffer += possibility;
        Possibilities = buffer;
    }
    
    public CellPossibilities(CellPossibility coord)
    {
        Cell = new Cell(coord.Row, coord.Column);
        var buffer = new ReadOnlyBitSet16();
        buffer += coord.Possibility;
        Possibilities = buffer;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not CellPossibilities cp) return false;
        return Cell == cp.Cell && Possibilities.Equals(cp.Possibilities);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Cell.GetHashCode(), Possibilities.GetHashCode());
    }

    public override string ToString()
    {
        return $"{Cell} => {Possibilities}";
    }
}

public readonly struct MiniGrid
{
    public MiniGrid(int miniRow, int miniColumn)
    {
        MiniRow = miniRow;
        MiniColumn = miniColumn;
    }

    public int MiniRow { get; }
    public int MiniColumn { get; }
}