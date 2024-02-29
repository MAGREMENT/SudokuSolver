﻿using System.Collections.Generic;
using Model.Helpers;
using Model.Helpers.Changes;
using Model.Helpers.Logs;
using Model.Sudoku.Solver;

namespace Model.Sudoku;

public interface ISolver
{
    public StrategyManager StrategyManager { get; }
    public void SetSudoku(Sudoku sudoku);
    public void SetState(ArraySolvingState state);
    public void Solve(bool stopAtProgress);
    public BuiltChangeCommit[] EveryPossibleNextStep();
    public ISolvingState CurrentState { get; }
    public ISolvingState StartState { get; }
    public IReadOnlyList<ISolverLog> Logs { get; }
    public void SetSolutionByHand(int number, int row, int col);
    public void RemoveSolutionByHand(int row, int col);
    public void RemovePossibilityByHand(int possibility, int row, int col);
    public void ApplyCommit(BuiltChangeCommit commit);
    
    
    public event OnLogsUpdate? LogsUpdated;
}

public delegate void OnLogsUpdate();