﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Model.Kakuros;
using Model.Utility;
using Model.Utility.BitSets;

namespace Model.Nonograms;

public class Nonogram : IReadOnlyNonogram
{
    private bool[,] _cells;
    private readonly INonogramLineCollection _horizontalCollection;
    private readonly INonogramLineCollection _verticalCollection;

    public int RowCount => _cells.GetLength(0);
    public int ColumnCount => _cells.GetLength(1);
    public IReadOnlyNonogramLineCollection HorizontalLineCollection => _horizontalCollection;
    public IReadOnlyNonogramLineCollection VerticalLineCollection => _verticalCollection;

    public Nonogram()
    {
        _cells = new bool[0, 0];
        _horizontalCollection = new ListListNonogramLineCollection();
        _verticalCollection = new ListListNonogramLineCollection();
    }

    private Nonogram(bool[,] cells, INonogramLineCollection horizontalCollection, INonogramLineCollection verticalCollection)
    {
        _cells = cells;
        _horizontalCollection = horizontalCollection;
        _verticalCollection = verticalCollection;
    }

    public bool Add(Orientation orientation, int index, params int[] values)
    {
        if (orientation == Orientation.Horizontal)
        {
            if (index < RowCount)
            {
                _horizontalCollection.SetValues(index, values);
            }
            else if (index == RowCount)
            {
                _horizontalCollection.AddValues(values);
                ResizeTo(RowCount + 1, ColumnCount);
            }
            else return false;
        }
        else
        {
            if (index < ColumnCount)
            {
                _verticalCollection.SetValues(index, values);
            }
            else if (index == ColumnCount)
            {
                _verticalCollection.AddValues(values);
                ResizeTo(RowCount, ColumnCount + 1);
            }
            else return false;
        }

        return true;
    }

    public bool Add(IEnumerable<NonogramLine> lines)
    {
        var hBitSet = new InfiniteBitSet();
        var vBitSet = new InfiniteBitSet();
        
        foreach (var line in lines)
        {
            if (line.Orientation == Orientation.Horizontal)
            {
                if (line.Index >= RowCount) hBitSet.Set(line.Index - RowCount);
            }
            else
            {
                if (line.Index >= ColumnCount) vBitSet.Set(line.Index - ColumnCount);
            }
        }

        if (hBitSet.Count > 0 && !hBitSet.IsFilledUntilLast()) return false;
        if (vBitSet.Count > 0 && !vBitSet.IsFilledUntilLast()) return false;
        
        //TODO 

        return true;
    }

    public void Add(IEnumerable<IEnumerable<int>> hValues, IEnumerable<IEnumerable<int>> vValues)
    {
        int rAdded = 0, cAdded = 0;
        foreach (var hv in hValues)
        {
            _horizontalCollection.AddValues(hv);
            rAdded++;
        }

        foreach (var vv in vValues)
        {
            _verticalCollection.AddValues(vv);
            cAdded++;
        }

        ResizeTo(RowCount + rAdded, ColumnCount + cAdded);
    }

    public Nonogram Copy()
    {
        var buffer = new bool[RowCount, ColumnCount];
        Array.Copy(_cells, buffer, _cells.Length);

        return new Nonogram(buffer, _horizontalCollection.Copy(), _verticalCollection.Copy());
    }

    public bool this[int row, int col]
    {
        get => _cells[row, col];
        set => _cells[row, col] = value;
    }

    public bool IsHorizontalLineCorrect(int index)
    {
        var val = _horizontalCollection[index];
        int cursor = 0;
        foreach (var v in val)
        {
            while (!_cells[index, cursor])
            {
                if (cursor >= ColumnCount) return false;
                cursor++;
            }

            for (; cursor < v; cursor++)
            {
                if (cursor >= ColumnCount || !_cells[index, cursor]) return false;
            }

            if (_cells[index, cursor]) return false;

            cursor++;
        }

        return true;
    }

    public bool IsVerticalLineCorrect(int index)
    {
        var val = _verticalCollection[index];
        int cursor = 0;
        foreach (var v in val)
        {
            while (!_cells[cursor, index])
            {
                if (cursor >= RowCount) return false;
                cursor++;
            }

            for (; cursor < v; cursor++)
            {
                if (cursor >= RowCount || !_cells[cursor, index]) return false;
            }

            if (_cells[cursor, index]) return false;

            cursor++;
        }

        return true;
    }

    public bool IsCorrect()
    {
        for (int row = 0; row < RowCount; row++)
        {
            if (!IsHorizontalLineCorrect(row)) return false;
        }

        for (int col = 0; col < ColumnCount; col++)
        {
            if (!IsVerticalLineCorrect(col)) return false;
        }

        return true;
    }

    public override string ToString()
    {
        var maxDepth = 0;
        var maxWidth = 0;

        for (int i = 0; i < _verticalCollection.Count; i++)
        {
            int c = 0;
            foreach (var v in _verticalCollection[i])
            {
                maxWidth = Math.Max(maxWidth, v.ToString().Length);
                c++;
            }

            maxDepth = Math.Max(maxDepth, c);
        }

        var maxTotalWidth = 0;

        for (int i = 0; i < _horizontalCollection.Count; i++)
        {
            var c = 0;
            foreach (var v in _horizontalCollection[i])
            {
                c += v.ToString().Length + 1;
            }

            maxTotalWidth = Math.Max(maxTotalWidth, c);
        }

        var builder = new StringBuilder();

        for (int vRow = maxDepth - 1; vRow >= 0; vRow--)
        {
            builder.Append(' '.Repeat(maxTotalWidth + 1));

            for (int col = 0; col < ColumnCount; col++)
            {
                var val = _verticalCollection.TryGetValue(col, vRow);
                if (val >= 0) val = _verticalCollection.TryGetValue(col, _verticalCollection.ValueCount(col) - vRow - 1);
                var s = val < 0 ? " " : val.ToString();
                builder.Append(s.FillEvenlyWith(' ', maxWidth) + ' ');
            }

            builder.Append('\n');
        }

        builder.Append(ToStringHorizontalLine(maxTotalWidth, maxWidth) + '\n');
        for (int row = 0; row < RowCount; row++)
        {
            var secondBuilder = new StringBuilder();
            bool first = true;
            foreach (var val in _horizontalCollection[row])
            {
                if (first) first = false;
                else secondBuilder.Append(' ');

                secondBuilder.Append(val);
            }

            var valAsString = secondBuilder.ToString();
            builder.Append(' '.Repeat(maxTotalWidth - valAsString.Length) + valAsString + '|');

            for (int col = 0; col < ColumnCount; col++)
            {
                var s = _cells[row, col] ? "0" : " ";
                builder.Append(s.FillEvenlyWith(' ', maxWidth) + '|');
            }

            builder.Append('\n' + ToStringHorizontalLine(maxTotalWidth, maxWidth) + '\n');
        }
        
        return builder.ToString();
    }

    private string ToStringHorizontalLine(int maxTotalWidth, int maxWidth)
    {
        return ' '.Repeat(maxTotalWidth) + ("+" + '-'.Repeat(maxWidth)).Repeat(ColumnCount) + '+';
    }

    private void ResizeTo(int rowCount, int colCount)
    {
        var buffer = new bool[rowCount, colCount];

        for (int row = 0; row < rowCount && row < RowCount; row++)
        {
            for (int col = 0; col < ColumnCount && col < colCount; col++)
            {
                buffer[row, col] = _cells[row, col];
            }
        }

        _cells = buffer;
    }
}

public interface IReadOnlyNonogram
{
    int RowCount { get; }
    int ColumnCount { get; }
    IReadOnlyNonogramLineCollection HorizontalLineCollection { get; }
    IReadOnlyNonogramLineCollection VerticalLineCollection { get; }
    bool this[int row, int col] { get; }
    bool IsHorizontalLineCorrect(int index);
    bool IsVerticalLineCorrect(int index);
    bool IsCorrect();
}

public readonly struct NonogramLine
{
    public NonogramLine(Orientation orientation, int index, int[] values)
    {
        Orientation = orientation;
        Index = index;
        Values = values;
    }

    public Orientation Orientation { get; }
    public int Index { get; }
    public int[] Values { get; }
}

public interface IReadOnlyNonogramLineCollection : IEnumerable<IEnumerable<int>>
{
    int Count { get; }
    IEnumerable<int> this[int index] { get; }
    IReadOnlyList<int> AsList(int index);
    int TryGetValue(int lineIndex, int valueIndex);
    int ValueCount(int index);
    int SpaceNeeded(int index);
    int TotalExpected(int index);
    INonogramLineCollection Copy();
}

public interface INonogramLineCollection : IReadOnlyNonogramLineCollection
{ 
    void SetValues(int index, IEnumerable<int> values);
    void AddValues(IEnumerable<int> values);
}

public class ListListNonogramLineCollection : INonogramLineCollection
{
    private readonly List<List<int>> _list;

    public int Count => _list.Count;

    public ListListNonogramLineCollection()
    {
        _list = new List<List<int>>();
    }

    private ListListNonogramLineCollection(List<List<int>> list)
    {
        _list = list;
    }
    
    public void SetValues(int index, IEnumerable<int> values)
    {
        var l = _list[index];
        l.Clear();
        l.AddRange(values);
    }

    public void AddValues(IEnumerable<int> values)
    { 
        _list.Add(new List<int>(values));
    }

    public IEnumerable<int> this[int index] => _list[index];

    public IReadOnlyList<int> AsList(int index)
    {
        return _list[index];
    }

    public int TryGetValue(int lineIndex, int valueIndex)
    {
        if (lineIndex < 0 || lineIndex >= _list.Count) return -1;

        var l = _list[lineIndex];
        if (valueIndex < 0 || valueIndex >= l.Count) return -1;

        return l[valueIndex];
    }

    public int ValueCount(int index)
    {
        return _list[index].Count;
    }

    public int SpaceNeeded(int index)
    {
        var l = _list[index];
        if (l.Count == 0) return 0;

        var total = l[0];
        for (int i = 1; i < l.Count; i++)
        {
            total += 1 + l[i];
        }

        return total;
    }

    public int TotalExpected(int index)
    {
        var total = 0;
        foreach (var val in _list[index]) total += val;
        return total;
    }

    public INonogramLineCollection Copy()
    {
        var buffer = new List<List<int>>();
        foreach (var l in _list)
        {
            buffer.Add(new List<int>(l));
        }

        return new ListListNonogramLineCollection(buffer);
    }

    public IEnumerator<IEnumerable<int>> GetEnumerator()
    {
        return _list.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

public readonly struct LineSpace
{
    public int Start { get; }
    public int End { get; }
    
    public LineSpace(int start, int end)
    {
        Start = start;
        End = end;
    }

    public int GetLength => End - Start + 1;

    public IEnumerable<Cell> EnumerateCells(Orientation orientation, int unit)
    {
        for (int i = Start; i <= End; i++)
        {
            yield return orientation == Orientation.Horizontal
                ? new Cell(unit, i)
                : new Cell(i, unit);
        }
    }
}