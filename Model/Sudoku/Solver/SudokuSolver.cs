﻿using System.Collections.Generic;
using Model.Helpers;
using Model.Helpers.Changes;
using Model.Helpers.Logs;
using Model.Sudoku.Solver.BitSets;
using Model.Sudoku.Solver.Position;
using Model.Sudoku.Solver.Possibility;
using Model.Sudoku.Solver.StrategiesUtility;
using Model.Sudoku.Solver.StrategiesUtility.AlmostLockedSets;

namespace Model.Sudoku.Solver;

//TODO => Documentation + Explanation + Review highlighting for each strategy
//TODO => For each strategy using old als, revamp
public class SudokuSolver : ISolver, IStrategyUser, ILogManagedChangeProducer, ILogProducer
{
    private Sudoku _sudoku;
    private readonly ReadOnlyBitSet16[,] _possibilities = new ReadOnlyBitSet16[9, 9];
    private readonly GridPositions[] _positions = new GridPositions[9];
    private readonly LinePositions[,] _rowsPositions = new LinePositions[9, 9];
    private readonly LinePositions[,] _colsPositions = new LinePositions[9, 9];
    private readonly MiniGridPositions[,,] _minisPositions = new MiniGridPositions[3,3,9];
    
    public IReadOnlySudoku Sudoku => _sudoku;
    public IReadOnlyList<ISolverLog> Logs => LogManager.Logs;
    
    public ChangeManagement ChangeManagement
    {
        init
        {
            switch (value)
            {
                case ChangeManagement.Fast :
                    ChangeBuffer = new FastChangeBuffer(this);
                    LogsManaged = false;
                    break;
                case ChangeManagement.WithLogs :
                    ChangeBuffer = new LogManagedChangeBuffer(this);
                    LogsManaged = true;
                    break;
            }
        }
    }
    public bool StatisticsTracked { get; init; }
    public bool UniquenessDependantStrategiesAllowed { get; private set; } = true;
    public bool LogsManaged { get; private init; }

    public SolverState CurrentState => new(this);
    public SolverState StartState { get; private set; }

    public delegate void SolutionAddition(int number, int row, int col);
    public event SolutionAddition? SolutionAdded;

    public delegate void PossibilityRemoval(int number, int row, int col);
    public event PossibilityRemoval? PossibilityRemoved;
    
    public event OnCurrentStrategyChange? CurrentStrategyChanged;

    public event OnLogsUpdate? LogsUpdated;

    private IReadOnlyList<ISudokuStrategy> Strategies => _strategyManager.Strategies;
    private int _currentStrategy = -1;
    
    private int _solutionAddedBuffer;
    private int _possibilityRemovedBuffer;

    private bool _startedSolving;

    private readonly StrategyManager _strategyManager;

    public SudokuSolver() : this(new Sudoku()) { }

    private SudokuSolver(Sudoku s)
    {
        _strategyManager = new StrategyManager();
        ChangeManagement = ChangeManagement.Fast;
        
        _sudoku = s;
        CallOnNewSudokuForEachStrategy();

        SolutionAdded += (_, _, _) => _solutionAddedBuffer++;
        PossibilityRemoved += (_, _, _) => _possibilityRemovedBuffer++;

        InitPossibilities();
        StartState = new SolverState(this);

        PreComputer = new PreComputer(this);
        
        LogManager = new LogManager(this);
        LogManager.LogsUpdated += () => LogsUpdated?.Invoke();
        
        ChangeBuffer = new LogManagedChangeBuffer(this);

        AlmostHiddenSetSearcher = new AlmostHiddenSetSearcher(this);
        AlmostNakedSetSearcher = new AlmostNakedSetSearcher(this);
    }

    public void Bind(IRepository<List<StrategyDAO>> repository)
    {
        _strategyManager.Bind(repository);
    }

    //Solver------------------------------------------------------------------------------------------------------------

    public IStrategyManager StrategyManager => _strategyManager;

    public void SetSudoku(Sudoku s)
    {
        _sudoku = s;
        CallOnNewSudokuForEachStrategy();

        ResetPossibilities();
        StartState = new SolverState(this);

        LogManager.Clear();
        PreComputer.Reset();

        _startedSolving = false;
    }

    public void SetState(SolverState state)
    {
        _sudoku = SudokuTranslator.TranslateTranslatable(state);
        CallOnNewSudokuForEachStrategy();
        
        ResetPossibilities();
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                var at = state.At(row, col);
                if (!at.IsPossibilities) continue;

                var asPoss = at.AsPossibilities;
                foreach (var p in PossibilitiesAt(row, col).EnumeratePossibilities())
                {
                    if (!asPoss.Contains(p)) RemovePossibility(p, row, col, false);
                }
            }
        }
        StartState = new SolverState(this);
        
        LogManager.Clear();
        PreComputer.Reset();

        _startedSolving = false;
    }
    
    public void SetSolutionByHand(int number, int row, int col)
    {
        if (_sudoku[row, col] != 0) RemoveSolution(row, col);

        if (_startedSolving && LogsManaged)
        {
            var stateBefore = CurrentState;
            if (!AddSolution(number, row, col, false)) return;
            LogManager.AddByHand(number, row, col, ChangeType.Solution, stateBefore);
        }
        else if (!AddSolution(number, row, col, false)) return;
        
        if(!_startedSolving) StartState = new SolverState(this);
    }

    public void RemoveSolutionByHand(int row, int col)
    {
        if (_startedSolving) return;

        RemoveSolution(row, col);
        StartState = new SolverState(this);
    }

    public void RemovePossibilityByHand(int possibility, int row, int col)
    {
        if (_startedSolving && LogsManaged)
        {
            var stateBefore = CurrentState;
            if (!RemovePossibility(possibility, row, col, false)) return;
            LogManager.AddByHand(possibility, row, col, ChangeType.Possibility, stateBefore);
        }
        else if (!RemovePossibility(possibility, row, col, false)) return;

        if (!_startedSolving) StartState = new SolverState(this);
    }
    
    public void Solve(bool stopAtProgress = false)
    {
        _startedSolving = true;
        
        for (_currentStrategy = 0; _currentStrategy < Strategies.Count; _currentStrategy++)
        {
            if (!_strategyManager.IsStrategyUsed(_currentStrategy)) continue;

            CurrentStrategyChanged?.Invoke(_currentStrategy);
            var current = Strategies[_currentStrategy];

            if (StatisticsTracked) current.Tracker.StartUsing();
            current.Apply(this);
            ChangeBuffer.Push(current);
            if (StatisticsTracked) current.Tracker.StopUsing(_solutionAddedBuffer, _possibilityRemovedBuffer);

            if (_solutionAddedBuffer + _possibilityRemovedBuffer == 0) continue;

            _solutionAddedBuffer = 0;
            _possibilityRemovedBuffer = 0;
            
            _currentStrategy = -1;
            CurrentStrategyChanged?.Invoke(_currentStrategy);

            PreComputer.Reset();

            if (stopAtProgress || _sudoku.IsComplete()) return;
        }

        _currentStrategy = -1;
        CurrentStrategyChanged?.Invoke(_currentStrategy);
    }

    public BuiltChangeCommit[] EveryPossibleNextStep()
    {
        var oldBuffer = ChangeBuffer;
        ChangeBuffer = new NotExecutedChangeBuffer(this);
        
        var realBehaviors = new Dictionary<int, OnCommitBehavior>();
        for (_currentStrategy = 0; _currentStrategy < Strategies.Count; _currentStrategy++)
        {
            if (!_strategyManager.IsStrategyUsed(_currentStrategy)) continue;
            
            CurrentStrategyChanged?.Invoke(_currentStrategy);
            var current = Strategies[_currentStrategy];

            realBehaviors[_currentStrategy] = current.OnCommitBehavior;
            current.OnCommitBehavior = OnCommitBehavior.WaitForAll;
            
            current.Apply(this);
            ChangeBuffer.Push(current);
        }
        
        _currentStrategy = -1;
        CurrentStrategyChanged?.Invoke(_currentStrategy);

        foreach (var entry in realBehaviors)
        {
            _strategyManager.Strategies[entry.Key].OnCommitBehavior = entry.Value;
        }

        var result = ((NotExecutedChangeBuffer)ChangeBuffer).DumpCommits();
        ChangeBuffer = oldBuffer;
        return result;
    }
    
    public void ApplyCommit(BuiltChangeCommit commit)
    {
        ChangeBuffer.PushCommit(commit);
    }

    public bool IsWrong()
    {
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                if (_sudoku[row, col] == 0 && _possibilities[row, col].Count == 0) return true;
            }
        }

        return false;
    }

    public void UseAllStrategies(bool yes)
    {
        _strategyManager.UseAllStrategies(yes);
    }

    public void ExcludeStrategy(int number)
    {
        _strategyManager.ExcludeStrategy(number);
    }

    public void UseStrategy(int number)
    {
        _strategyManager.UseStrategy(number);
    }

    public void AllowUniqueness(bool yes)
    {
        UniquenessDependantStrategiesAllowed = yes;
        _strategyManager.AllowUniqueness(yes);
    }
    
    public StrategyInformation[] GetStrategyInfo()
    {
        return _strategyManager.GetStrategiesInformation();
    }
    
    public ReadOnlyBitSet16 RawPossibilitiesAt(int row, int col)
    {
        if (Sudoku[row, col] != 0) return new ReadOnlyBitSet16();
        
        var result = ReadOnlyBitSet16.Filled(1, 9);

        var startR = row / 3 * 3;
        var startC = col / 3 * 3;
        for (int u = 0; u < 9; u++)
        {
            if (u != row) result -= Sudoku[u, col];
            if (u != col) result -= Sudoku[row, u];

            var r = startR + u / 3;
            var c = startC + u % 3;
            if (r != row || c != col) result -= Sudoku[r, c];
        }

        return result;
    }
    
    //PossibilityHolder-------------------------------------------------------------------------------------------------
    
    public ReadOnlyBitSet16 PossibilitiesAt(int row, int col)
    {
        return _possibilities[row, col];
    }
    
    public IReadOnlyLinePositions RowPositionsAt(int row, int number)
    {
        return _rowsPositions[row, number - 1];
    }
    
    public IReadOnlyLinePositions ColumnPositionsAt(int col, int number)
    {
        return _colsPositions[col, number - 1];
    }
    
    public IReadOnlyMiniGridPositions MiniGridPositionsAt(int miniRow, int miniCol, int number)
    {
        return _minisPositions[miniRow, miniCol, number - 1];
    }

    public IReadOnlyGridPositions PositionsFor(int number)
    {
        return _positions[number - 1];
    }

    //StrategyManager---------------------------------------------------------------------------------------------------

    public IChangeBuffer ChangeBuffer { get; private set; }
    public PreComputer PreComputer { get; }
    public AlmostHiddenSetSearcher AlmostHiddenSetSearcher { get; }
    public AlmostNakedSetSearcher AlmostNakedSetSearcher { get; }

    //ChangeManager-----------------------------------------------------------------------------------------------------
    
    public IPossibilitiesHolder TakeSnapshot()
    {
        return SolverSnapshot.TakeSnapshot(this);
    }

    public bool CanRemovePossibility(CellPossibility cp)
    {
        return PossibilitiesAt(cp.Row, cp.Column).Contains(cp.Possibility);
    }

    public bool CanAddSolution(CellPossibility cp)
    {
        return Sudoku[cp.Row, cp.Column] == 0;
    }

    public bool ExecuteChange(SolverChange change)
    {
        return change.ChangeType == ChangeType.Solution
            ? AddSolution(change.Number, change.Row, change.Column)
            : RemovePossibility(change.Number, change.Row, change.Column);
    }

    public LogManager LogManager { get; }

    //Private-----------------------------------------------------------------------------------------------------------

    private void CallOnNewSudokuForEachStrategy()
    {
        foreach (var s in Strategies)
        {
            s.OnNewSudoku(_sudoku);
        }
    }
    
    private bool AddSolution(int number, int row, int col, bool callEvents = true)
    {
        if (!_possibilities[row, col].Contains(number)) return false;
        
        _sudoku[row, col] = number;
        UpdatePossibilitiesAfterSolutionAdded(number, row, col);
        
        if(callEvents) SolutionAdded?.Invoke(number, row, col);
        return true;
    }

    private void RemoveSolution(int row, int col)
    {
        if (_sudoku[row, col] == 0) return;

        _sudoku[row, col] = 0;
        ResetPossibilities();
    }

    private bool RemovePossibility(int possibility, int row, int col, bool callEvents = true)
    {
        if (!_possibilities[row, col].Contains(possibility)) return false;

        _possibilities[row, col] -= possibility;
        _positions[possibility - 1].Remove(row, col);
        _rowsPositions[row, possibility - 1].Remove(col);
        _colsPositions[col, possibility - 1].Remove(row);
        _minisPositions[row / 3, col / 3, possibility - 1].Remove(row % 3, col % 3);
        
        if(callEvents) PossibilityRemoved?.Invoke(possibility, row, col);
        return true;
    }

    private void InitPossibilities()
    {
        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                _possibilities[i, j] = ReadOnlyBitSet16.Filled(1, 9);
                _rowsPositions[i, j] = LinePositions.Filled();
                _colsPositions[i, j] = LinePositions.Filled();
                _minisPositions[i / 3, i % 3, j] = MiniGridPositions.Filled(i / 3, i % 3);
            }
            
            _positions[i] = GridPositions.Filled();
        }
        
        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                if (_sudoku[i, j] != 0)
                {
                    UpdatePossibilitiesAfterSolutionAdded(_sudoku[i, j], i, j);
                }
            }
        }
    }

    private void ResetPossibilities()
    {
        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                _possibilities[i, j] = ReadOnlyBitSet16.Filled(1, 9);
                _rowsPositions[i, j].Fill();
                _colsPositions[i, j].Fill();
                _minisPositions[i / 3, i % 3, j].Fill();
            }
            
            _positions[i].Fill();
        }
        
        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                if (_sudoku[i, j] != 0)
                {
                    UpdatePossibilitiesAfterSolutionAdded(_sudoku[i, j], i, j);
                }
            }
        }
    }

    private void UpdatePossibilitiesAfterSolutionAdded(int number, int row, int col)
    {
        int miniRow = row / 3,
            miniCol = col / 3,
            gridRow = row % 3,
            gridCol = col % 3,
            startRow = miniRow * 3,
            startCol = miniCol * 3;

        _possibilities[row, col] = new ReadOnlyBitSet16();
        for (int i = 0; i < 9; i++)
        {
            _positions[i].Remove(row, col);
            _rowsPositions[row, i].Remove(col);
            _colsPositions[col, i].Remove(row);
            _minisPositions[miniRow, miniCol, i].Remove(gridRow, gridCol);
        }
        
        for (int i = 0; i < 9; i++)
        {
            RemovePossibilityCheckLess(number, row, i);
            RemovePossibilityCheckLess(number, i, col);
            RemovePossibilityCheckLess(number,  startRow + i / 3, startCol + i % 3);
        }
    }
    
    private void RemovePossibilityCheckLess(int possibility, int row, int col)
    {
        _possibilities[row, col] -= possibility;
        _positions[possibility - 1].Remove(row, col);
        _rowsPositions[row, possibility - 1].Remove(col);
        _colsPositions[col, possibility - 1].Remove(row);
        _minisPositions[row / 3, col / 3, possibility - 1].Remove(row % 3, col % 3);
    }
}

public enum ChangeManagement
{
    WithLogs, Fast
}

