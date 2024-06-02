using System.Threading.Tasks;
using Model.Helpers;
using Model.Kakuros;
using Model.Kakuros.Strategies;
using Model.Utility;
using Model.Utility.BitSets;

namespace DesktopApplication.Presenter.Kakuros.Solve;

public class KakuroSolvePresenter
{
    private readonly IKakuroSolveView _view;
    private readonly KakuroSolver _solver;

    private Cell? _selectedCell;
    private IKakuroSum? _selectedSum;
    private int _bufferedAmount = -1;

    public KakuroSolvePresenter(IKakuroSolveView view)
    {
        _view = view;
        _solver = new KakuroSolver(new RecursiveKakuroCombinationCalculator());
        _solver.StrategyManager.AddStrategies(new NakedSingleStrategy(),
            new AmountCoherencyStrategy(),
            new CombinationCoherencyStrategy());
    }
    
    public void OnKakuroAsStringBoxShowed()
    {
        _view.SetKakuroAsString(KakuroTranslator.TranslateSumFormat(_solver.Kakuro));
    }

    public void SetNewKakuro(string s)
    {
        var k = KakuroTranslator.TranslateSumFormat(s);
        _solver.SetKakuro(k);
        ShowNewKakuro(k);
    }

    public async void Solve(bool stopAtProgress)
    {
        _solver.StrategyEnded += OnProgressMade;
        await Task.Run(() => _solver.Solve(stopAtProgress));
        _solver.StrategyEnded -= OnProgressMade;
    }

    public void SelectCell(int row, int col)
    {
        EnterAmount();
        
        var cell = new Cell(row, col);
        if (cell == _selectedCell)
        {
            _selectedCell = null;
            _view.Drawer.ClearCursor();
        }
        else
        {
            _selectedCell = cell;
            _view.Drawer.PutCursorOnNumberCell(row, col);
        }
        
        _view.Drawer.Refresh();
    }

    public void SelectSum(int row, int col)
    {
        _selectedCell = null;
        
        IKakuroSum? sum = null;
        foreach (var s in _solver.Kakuro.Sums)
        {
            var cell = s.GetAmountCell();
            if (cell.Row == row && cell.Column == col)
            {
                sum = s;
                break;
            }
        }

        if (sum is null || sum.Equals(_selectedSum)) EnterAmount();
        else
        {
            _selectedSum = sum;
            _bufferedAmount = sum.Amount;
            _view.Drawer.PutCursorOnAmountCell(row, col, sum.Orientation);
        }
        
        _view.Drawer.Refresh();
    }

    public void AddCell()
    {
        if (_selectedSum is null) return;

        var copy = _solver.Kakuro.Copy();
        if (!copy.AddCellTo(_selectedSum)) return;
        
        _solver.SetKakuro(copy);
        ShowNewKakuro(copy);
    }

    public void RemoveCell()
    {
        if (_selectedSum is null) return;

        var copy = _solver.Kakuro.Copy();
        if(!copy.RemoveCellFrom(_selectedSum)) return;
        
        _solver.SetKakuro(copy);
        ShowNewKakuro(copy);
    }

    public void AddDigitToAmount(int n)
    {
        if (_selectedSum is null) return;
        
        _bufferedAmount *= 10;
        _bufferedAmount += n;

        var cell = _selectedSum.GetAmountCell();
        _view.Drawer.ReplaceAmount(cell.Row, cell.Column, _bufferedAmount, _selectedSum.Orientation);
        _view.Drawer.Refresh();
    }

    public void RemoveDigitFromAmount()
    {
        if (_selectedSum is null) return;
        
        _bufferedAmount /= 10;
        
        var cell = _selectedSum.GetAmountCell();
        _view.Drawer.ReplaceAmount(cell.Row, cell.Column, _bufferedAmount, _selectedSum.Orientation);
        _view.Drawer.Refresh();
    }

    public void EnterAmount()
    {
        if (_selectedSum is null) return;

        var copy = _solver.Kakuro.Copy();
        bool success = copy.ReplaceAmount(_selectedSum, _bufferedAmount);
        
        _selectedSum = null;
        _bufferedAmount = -1;
        _view.Drawer.ClearCursor();

        if (success)
        {
            _solver.SetKakuro(copy);
            ShowNewKakuro(copy);
        }
        else ShowState(_solver);
    }

    private void ShowNewKakuro(IKakuro k)
    {
        var drawer = _view.Drawer;

        drawer.RowCount = k.RowCount;
        drawer.ColumnCount = k.ColumnCount;

        drawer.ClearNumbers();
        drawer.ClearAmounts();
        drawer.ClearPresence();
        foreach (var sum in k.Sums)
        {
            foreach (var cell in sum)
            {
                drawer.SetPresence(cell.Row, cell.Column, true);
                var n = k[cell];
                if (n != 0) drawer.SetSolution(cell.Row, cell.Column, n);
                else drawer.SetPossibilities(cell.Row, cell.Column, 
                    _solver.PossibilitiesAt(cell.Row, cell.Column).EnumeratePossibilities());
            }

            var c = sum.GetAmountCell();
            drawer.SetPresence(c.Row, c.Column, sum.Orientation, true);
            drawer.SetAmount(c.Row, c.Column, sum.Amount, sum.Orientation);
        }
        
        drawer.RedrawLines();
        drawer.Refresh();
    }

    private void OnProgressMade(KakuroStrategy strategy, int index, int solutionAdded, int possibilitiesRemoved)
    {
        if(solutionAdded + possibilitiesRemoved > 0) ShowState(_solver);
    } 

    private void ShowState(ISolvingState state)
    {
        var drawer = _view.Drawer;
        
        drawer.ClearNumbers();
        foreach (var cell in _solver.Kakuro.EnumerateCells())
        {
            var n = state[cell.Row, cell.Column];
            if (n == 0) drawer.SetPossibilities(cell.Row, cell.Column,
                    state.PossibilitiesAt(cell).EnumeratePossibilities());
            else drawer.SetSolution(cell.Row, cell.Column, n);
        }
        
        drawer.Refresh();
    }
}