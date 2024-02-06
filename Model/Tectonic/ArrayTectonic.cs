﻿using System;
using System.Collections.Generic;
using System.Text;
using Model.Utility;

namespace Model.Tectonic;

public class ArrayTectonic : ITectonic
{
    private readonly TectonicCell?[,] _cells;
    private readonly Zone[] _zones;
    
    public ArrayTectonic(int rowCount, int colCount, Zone[] zones)
    {
        _cells = new TectonicCell?[rowCount, colCount];
        _zones = zones;

        for (int i = 0; i < zones.Length; i++)
        {
            foreach (var cell in zones[i])
            {
                _cells[cell.Row, cell.Column] = new TectonicCell(i);
            }
        }
    }

    public int RowCount => _cells.GetLength(0);
    public int ColumnCount => _cells.GetLength(1);
    public IReadOnlyList<Zone> Zones => _zones;

    public int this[int row, int col]
    {
        get => _cells[row, col] is null ? 0 : _cells[row, col]!.Number;
        set
        {
            if (_cells[row, col] is not null) _cells[row, col]!.Number = value;
        }
    }

    public Zone GetZone(Cell cell)
    {
        return IsValid(cell) ? _zones[_cells[cell.Row, cell.Column]!.Zone] : Zone.Empty();
    }

    public IEnumerable<Cell> GetNeighbors(Cell cell)
    {
        var result = new Cell(cell.Row - 1, cell.Column);
        if (IsValid(result)) yield return result;

        result = new Cell(cell.Row - 1, cell.Column - 1);
        if (IsValid(result)) yield return result;
        
        result = new Cell(cell.Row, cell.Column - 1);
        if (IsValid(result)) yield return result;
        
        result = new Cell(cell.Row + 1, cell.Column - 1);
        if (IsValid(result)) yield return result;
        
        result = new Cell(cell.Row + 1, cell.Column);
        if (IsValid(result)) yield return result;
        
        result = new Cell(cell.Row + 1, cell.Column + 1);
        if (IsValid(result)) yield return result;
        
        result = new Cell(cell.Row, cell.Column + 1);
        if (IsValid(result)) yield return result;
        
        result = new Cell(cell.Row - 1, cell.Column + 1);
        if (IsValid(result)) yield return result;
    }

    public bool ShareAZone(Cell c1, Cell c2)
    {
        return GetZone(c1).Contains(c2);
    }

    public IEnumerable<Cell> EachCell()
    {
        for (int row = 0; row < _cells.GetLength(0); row++)
        {
            for (int col = 0; col < _cells.GetLength(1); col++)
            {
                if (_cells[row, col] is not null) yield return new Cell(row, col);
            }
        }
    }

    public IEnumerable<CellNumber> EachCellNumber()
    {
        for (int row = 0; row < _cells.GetLength(0); row++)
        {
            for (int col = 0; col < _cells.GetLength(1); col++)
            {
                if (_cells[row, col] is not null)
                    yield return new CellNumber(new Cell(row, col), _cells[row, col]!.Number);
            }
        }
    }

    public override string ToString()
    {
        StringBuilder builder = new();

        for (int row = 0; row < RowCount; row++)
        {
            if (row == 0) builder.Append(StringUtility.Repeat("+---", ColumnCount) + "+\n");
            else
            {
                for (int col = 0; col < ColumnCount; col++)
                {
                    builder.Append(ShareAZone(new Cell(row, col), new Cell(row - 1, col))
                        ? "+   "
                        : "+---");
                }

                builder.Append("+\n");
            }

            for (int col = 0; col < ColumnCount; col++)
            {
                var c = _cells[row, col];
                var n = c is null || c.Number == 0 ? " " : c.Number.ToString();
                if (col == 0 || !ShareAZone(new Cell(row, col), new Cell(row, col - 1))) builder.Append($"| {n} ");
                else builder.Append($"  {n} ");
            }

            builder.Append("|\n");
        }
        
        builder.Append(StringUtility.Repeat("+---", ColumnCount) + "+\n");

        return builder.ToString();
    }

    private bool IsValid(Cell cell)
    {
        return cell.Row >= 0 && cell.Row < _cells.GetLength(0)
                            && cell.Column >= 0 && cell.Column < _cells.GetLength(1)
                            && _cells[cell.Row, cell.Column] != null;
    }
    
    
}

public class TectonicCell
{
    public int Number { get; set; }
    public int Zone { get; }

    public TectonicCell(int zone)
    {
        Zone = zone;
    }
}