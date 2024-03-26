﻿using System.Collections.Generic;
using Model;
using Model.Helpers.Changes;
using Model.Sudoku.Solver.StrategiesUtility.Graphs;
using Model.Tectonic;
using Model.Utility;

namespace DesktopApplication.Presenter.Tectonic.Solve;

public interface ITectonicDrawer
{
    int RowCount { set; }
    int ColumnCount { set; }

    void Refresh();
    void ClearNumbers();
    void ShowSolution(int row, int column, int number);
    void ShowPossibilities(int row, int column, IEnumerable<int> possibilities);
    void ClearBorderDefinitions();
    void AddBorderDefinition(int insideRow, int insideColumn, BorderDirection direction, bool isThin);
    void PutCursorOn(Cell cell);
    void PutCursorOn(IZone cells);
    void PutCursorOn(IReadOnlyList<Cell> cells);
    void ClearCursor();
    void ClearHighlights();
    void FillPossibility(int row, int col, int possibility, ChangeColoration coloration);
    void FillCell(int row, int col, ChangeColoration coloration);
    void CreateLink(int rowFrom, int colFrom, int possibilityFrom, int rowTo, int colTo, int possibilityTo,
        LinkStrength strength, LinkOffsetSidePriority priority);
}

public enum BorderDirection
{
    Horizontal, Vertical
}