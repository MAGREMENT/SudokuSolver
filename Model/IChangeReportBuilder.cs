﻿using System.Collections.Generic;
using Model.StrategiesUtil;

namespace Model;

public interface IChangeReportBuilder
{
    public static void HighlightChanges(IHighlighter highlighter, List<SolverChange> changes)
    {
        foreach (var change in changes)
        {
            if(change.NumberType == SolverNumberType.Possibility)
                highlighter.HighlightPossibility(change.Number, change.Row, change.Column, ChangeColoration.ChangeTwo);
            else highlighter.HighlightCell(change.Row, change.Column, ChangeColoration.ChangeOne);
        }
    }

    public static string ChangesToString(List<SolverChange> changes)
    {
        var s = "";
        foreach (var change in changes)
        {
            var action = change.NumberType == SolverNumberType.Possibility
                ? "removed from the possibilities"
                : "added as definitive";
            s += $"[{change.Row + 1}, {change.Column + 1}] {change.Number} {action}\n";
        }

        return s;
    }

    public ChangeReport Build(List<SolverChange> changes, IChangeManager manager);
}

public interface IHighlighter
{
    public void HighlightPossibility(int possibility, int row, int col, ChangeColoration coloration);

    public void HighlightCell(int row, int col, ChangeColoration coloration);

    public void HighlightLinkGraphElement(ILinkGraphElement element, ChangeColoration coloration);

    public void CreateLink(PossibilityCoordinate from, PossibilityCoordinate to, LinkStrength linkStrength);

    public void CreateLink(ILinkGraphElement from, ILinkGraphElement to, LinkStrength linkStrength);
}

public enum ChangeColoration
{
    ChangeOne, ChangeTwo, CauseOffOne, CauseOffTwo, CauseOffThree, CauseOffFour, CauseOffFive, CauseOffSix, CauseOnOne, Neutral
}

public delegate void HighlightSolver(IHighlighter h);