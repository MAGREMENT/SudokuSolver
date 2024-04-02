﻿using System.Collections.Generic;
using Model.Helpers.Changes;
using Model.Sudoku.Solver.StrategiesUtility;
using Model.Sudoku.Solver.StrategiesUtility.Graphs;

namespace DesktopApplication.Presenter.Sudoku.Solve;

public interface ISudokuSolverDrawer : ISudokuDrawer
{
    void ShowPossibilities(int row, int col, IEnumerable<int> possibilities);
    void FillPossibility(int row, int col, int possibility, ChangeColoration coloration);
    void FillCell(int row, int col, ChangeColoration coloration);
    void EncirclePossibility(int row, int col, int possibility);
    void DrawPossibilityPatch(CellPossibility[] cps, ChangeColoration coloration);
    void EncircleCell(int row, int col);
    void EncircleRectangle(int rowFrom, int colFrom, int possibilityFrom, int rowTo, int colTo,
        int possibilityTo, ChangeColoration coloration);
    void EncircleRectangle(int rowFrom, int colFrom, int rowTo, int colTo, ChangeColoration coloration);
    void CreateLink(int rowFrom, int colFrom, int possibilityFrom, int rowTo, int colTo, int possibilityTo,
        LinkStrength strength, LinkOffsetSidePriority priority);
}