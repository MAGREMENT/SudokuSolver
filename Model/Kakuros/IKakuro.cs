﻿using System.Collections.Generic;
using Model.Utility;

namespace Model.Kakuros;

public interface IKakuro
{
    int RowCount { get; }
    int ColumnCount { get; }
    IReadOnlyList<IKakuroSum> Sums { get; }

    IEnumerable<IKakuroSum> SumsFor(Cell cell);
    IKakuroSum? VerticalSumFor(Cell cell);
    IKakuroSum? HorizontalSumFor(Cell cell);
    
    bool AddSum(IKakuroSum sum);
    void AddSumUnchecked(IKakuroSum sum);
}

public interface IKakuroSum : IEnumerable<Cell>
{
    Orientation Orientation { get; }
    int Amount { get; }
    int Length { get; }

    int GetFarthestRow();
    int GetFarthestColumn();
}

public enum Orientation
{
    Vertical, Horizontal
}