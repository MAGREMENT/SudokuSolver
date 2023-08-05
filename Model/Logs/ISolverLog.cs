﻿using System.Collections.Generic;

namespace Model.Logs;

public interface ISolverLog
{
    public int Id { get; }
    public string Title { get; }
    public Intensity Intensity { get; }
    public string Changes { get; }
    public string Explanation { get; }
    public string SolverState { get; }
    public HighLightCause CauseHighLighter { get; }

}

public enum Intensity
{
    Zero, One, Two, Three, Four, Five, Six
}

public enum SolverNumberType
{
    Possibility, Definitive
}

public class LogChange
{
    public LogChange(SolverNumberType numberType, int number, int row, int column)
    {
        NumberType = numberType;
        Number = number;
        Row = row;
        Column = column;
    }

    public SolverNumberType NumberType { get; }
    public int Number { get; }
    public int Row { get; }
    public int Column { get; }

    public override string ToString()
    {
        string action = NumberType == SolverNumberType.Definitive ? "added as definitive" : "removed from possibilities";
        return $"[{Row + 1}, {Column + 1}] {Number} {action}";
    }
}