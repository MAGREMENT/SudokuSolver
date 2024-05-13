﻿using Model.Helpers;
using Model.Helpers.Changes;
using Model.Helpers.Changes.Buffers;
using Model.Helpers.Highlighting;
using Model.Kakuros.Strategies;
using Model.Tectonics;
using Model.Utility;
using Model.Utility.BitSets;

namespace Model.Kakuros;

public class KakuroSolver : IKakuroStrategyUser, IChangeProducer
{
    private IKakuro _kakuro = new ArrayKakuro();
    private ReadOnlyBitSet16[,] _possibilities = new ReadOnlyBitSet16[0, 0];

    private readonly IKakuroCombinationCalculator _combinationCalculator;
    private readonly KakuroStrategy[] _strategies = {
        new NakedSingleStrategy()
    };

    private bool _wasChangeMade;
    
    public IChangeBuffer<IUpdatableSolvingState, ISolvingStateHighlighter> ChangeBuffer { get; set; }

    public event OnProgressMade? ProgressMade;

    public KakuroSolver(IKakuroCombinationCalculator combinationCalculator)
    {
        _combinationCalculator = combinationCalculator;
        ChangeBuffer = new FastChangeBuffer<IUpdatableSolvingState, ISolvingStateHighlighter>(this);
    }

    public IReadOnlyKakuro Kakuro => _kakuro;

    public void SetKakuro(IKakuro kakuro)
    {
        _kakuro = kakuro;
        _possibilities = new ReadOnlyBitSet16[kakuro.RowCount, kakuro.ColumnCount];
        InitPossibilities();
    }

    public void Solve(bool stopAtProgress)
    {
        for (int i = 0; i < _strategies.Length; i++)
        {
            var s = _strategies[i];

            s.Apply(this);
            ChangeBuffer.Push(s);

            if (!_wasChangeMade) continue;

            _wasChangeMade = false;
            i = -1;
            ProgressMade?.Invoke();

            if (stopAtProgress || _kakuro.IsComplete()) return;
        }
    }

    public int this[int row, int col] => _kakuro[row, col];

    public ReadOnlyBitSet16 PossibilitiesAt(int row, int col) => _possibilities[row, col];

    public bool CanRemovePossibility(CellPossibility cp) => _possibilities[cp.Row, cp.Column].Contains(cp.Possibility);

    public bool CanAddSolution(CellPossibility cp) => _possibilities[cp.Row, cp.Column].Contains(cp.Possibility)
                                                      && _kakuro[cp.Row, cp.Column] == 0;

    public bool ExecuteChange(SolverProgress progress)
    {
        return progress.ProgressType == ProgressType.PossibilityRemoval 
            ? RemovePossibility(progress.Row, progress.Column, progress.Number) 
            : AddSolution(progress.Row, progress.Column, progress.Number);
    }

    public void FakeChange()
    {
        _wasChangeMade = true;
    }

    #region Private

    private void InitPossibilities()
    {
        foreach (var cell in _kakuro.EnumerateCells())
        {
            _possibilities[cell.Row, cell.Column] = ReadOnlyBitSet16.Filled(1, 9);
        }

        foreach (var sum in _kakuro.Sums)
        {
            var pos = _combinationCalculator.CalculatePossibilities(sum.Amount, sum.Length);
            foreach (var cell in sum)
            {
                _possibilities[cell.Row, cell.Column] &= pos;
            }
        }
    }

    private bool RemovePossibility(int row, int col, int n)
    {
        if (!_possibilities[row, col].Contains(n)) return false;

        _possibilities[row, col] -= n;
        _wasChangeMade = true;
        return true;
    }

    private bool AddSolution(int row, int col, int n)
    {
        if (_kakuro[row, col] != 0) return false;

        _possibilities[row, col] = new ReadOnlyBitSet16();
        _kakuro[row, col] = n;
        UpdatePossibilitiesAfterSolutionAdded(row, col, n);
        _wasChangeMade = true;

        return true;
    }

    private void UpdatePossibilitiesAfterSolutionAdded(int row, int col, int n)
    {
        foreach (var sum in _kakuro.SumsFor(new Cell(row, col)))
        {
            var pos = _combinationCalculator.CalculatePossibilities(sum.Amount,
                sum.Length, _kakuro.GetSolutions(sum));

            foreach (var cell in sum)
            {
                if (cell.Row == row && cell.Column == col) continue;

                _possibilities[cell.Row, cell.Column] &= pos;
                _possibilities[cell.Row, cell.Column] -= n;
            }
        }
    }

    #endregion
}