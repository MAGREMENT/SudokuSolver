﻿using System.Collections.Generic;
using Model.Helpers.Changes;
using Model.Helpers.Highlighting;
using Model.Sudoku.Solver;
using Model.Sudoku.Solver.Explanation;

namespace Model.Helpers.Logs;

public interface ISolverLog
{
    public int Id { get; }
    public string Title { get; }
    public Intensity Intensity { get; }
    public IReadOnlyList<SolverChange> Changes { get; }
    public string Description { get; }
    public ExplanationElement? Explanation { get; }
    public SolverState StateBefore { get; }
    public SolverState StateAfter { get; }
    public HighlightManager HighlightManager { get; }
    public bool FromSolving { get; }
}

public enum Intensity
{
    Zero, One, Two, Three, Four, Five, Six
}