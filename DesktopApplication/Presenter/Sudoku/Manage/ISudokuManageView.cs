﻿using System.Collections.Generic;
using Model.Sudoku.Solver;

namespace DesktopApplication.Presenter.Sudoku.Manage;

public interface ISudokuManageView
{
    public void ClearSearchResults();
    public void AddSearchResult(string s);
    public void SetStrategyList(IReadOnlyList<SudokuStrategy> list);
}