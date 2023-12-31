using System.Collections.Generic;
using Global;
using Global.Enums;
using Model.Solver.Helpers.Changes;
using Model.Solver.Position;
using Model.Solver.Possibility;
using Model.Solver.StrategiesUtility;

namespace Model.Solver.Strategies;

public class NonColorablePatternStrategy : AbstractStrategy
{
    public const string OfficialName = "Non-Colorable Pattern";
    private const OnCommitBehavior DefaultBehavior = OnCommitBehavior.Return;

    public override OnCommitBehavior DefaultOnCommitBehavior => DefaultBehavior;

    private int _minPossCount;
    private int _maxPossCount;
    private int _maxNotInPatternCell;
    
    public NonColorablePatternStrategy(int minPossCount, int maxPossCount, int maxNotInPatternCell) : base(OfficialName, StrategyDifficulty.Extreme, DefaultBehavior)
    {
        _minPossCount = minPossCount;
        _maxPossCount = maxPossCount;
        _maxNotInPatternCell = maxNotInPatternCell;
        ArgumentsList.Add(new MinMaxIntStrategyArgument("Possibility count", 2, 5, 2, 5, 1,
            () => _minPossCount, i => _minPossCount = i, () => _maxPossCount, i => _maxPossCount = i));
        ArgumentsList.Add(new IntStrategyArgument("Max out of pattern cells", () => _maxNotInPatternCell,
            i => _maxNotInPatternCell = i, new SliderViewInterface(1, 5, 1)));
    }

    
    public override void Apply(IStrategyManager strategyManager)
    {
        List<Cell> perfect = new();
        List<Cell> notPerfect = new();
        var poss = Possibilities.NewEmpty();

        for (int possCount = _minPossCount; possCount <= _maxPossCount; possCount++)
        {
            foreach (var combination in CombinationCalculator.EveryCombinationWithSpecificCount(3,
                         CombinationCalculator.NumbersSample))
            {
                foreach (var i in combination)
                {
                    poss.Add(i);
                }

                for (int row = 0; row < 9; row++)
                {
                    for (int col = 0; col < 9; col++)
                    {
                        var p = strategyManager.PossibilitiesAt(row, col);
                        if (p.Count == 0) continue;

                        var cell = new Cell(row, col);
                        if (poss.PeekAll(p)) perfect.Add(cell);
                        else if (poss.PeekAny(p)) notPerfect.Add(cell);
                    }
                }

                if (perfect.Count > possCount && Try(strategyManager, perfect, notPerfect, poss)) return;
                
                poss.RemoveAll();
                perfect.Clear();
                notPerfect.Clear();
            }
        }
    }

    private bool Try(IStrategyManager strategyManager, List<Cell> perfect, List<Cell> notPerfect, Possibilities poss)
    {
        List<CellPossibility> outPossibilities = new();
        foreach (var combination in
                 CombinationCalculator.EveryCombinationWithMaxCount(_maxNotInPatternCell, notPerfect))
        {
            foreach (var cell in combination)
            {
                foreach (var p in strategyManager.PossibilitiesAt(cell))
                {
                    if (!poss.Peek(p)) outPossibilities.Add(new CellPossibility(cell, p));
                }
            }

            var targets = outPossibilities.Count == 1 
                ? outPossibilities 
                : Cells.SharedSeenExistingPossibilities(strategyManager, outPossibilities);
            if (targets.Count == 0 || IsPatternValid(perfect, combination, poss.Count))
            {
                outPossibilities.Clear();
                continue;
            }

            foreach (var cp in targets)
            {
                if(outPossibilities.Count == 1) strategyManager.ChangeBuffer.ProposeSolutionAddition(outPossibilities[0]);
                else strategyManager.ChangeBuffer.ProposePossibilityRemoval(cp);
            }

            if (strategyManager.ChangeBuffer.NotEmpty() && strategyManager.ChangeBuffer.Commit(this,
                    new NonColorablePatternReportBuilder(perfect.ToArray(), combination, poss.Copy())) &&
                        OnCommitBehavior == OnCommitBehavior.Return) return true;
        }
        
        return false;
    }

    private bool IsPatternValid(IReadOnlyList<Cell> one, IReadOnlyList<Cell> two, int count)
    {
        var forbidden = new GridPositions[count];
        for (int i = 0; i < count; i++)
        {
            forbidden[i] = new GridPositions();
        }

        forbidden[0].Add(one[0]);
        return ValiditySearch(one, two, 1, forbidden);
    }

    private bool ValiditySearch(IReadOnlyList<Cell> one, IReadOnlyList<Cell> two, int cursor, GridPositions[] forbidden)
    {
        Cell current;
        if (cursor < one.Count) current = one[cursor];
        else if (cursor < one.Count + two.Count) current = two[cursor - one.Count];
        else return true;

        foreach (var f in forbidden)
        {
            if (f.RowCount(current.Row) > 0
                || f.ColumnCount(current.Column) > 0
                || f.MiniGridCount(current.Row / 3, current.Column / 3) > 0) continue;

            f.Add(current);
            if (ValiditySearch(one, two, cursor + 1, forbidden)) return true;
            f.Remove(current);
        }
        
        return false;
    }
}

public class NonColorablePatternReportBuilder : IChangeReportBuilder
{
    private readonly IReadOnlyList<Cell> _perfect;
    private readonly IReadOnlyList<Cell> _notPerfect;
    private readonly Possibilities _possibilities;

    public NonColorablePatternReportBuilder(IReadOnlyList<Cell> perfect, IReadOnlyList<Cell> notPerfect, Possibilities possibilities)
    {
        _perfect = perfect;
        _notPerfect = notPerfect;
        _possibilities = possibilities;
    }

    public ChangeReport Build(List<SolverChange> changes, IPossibilitiesHolder snapshot)
    {
        return new ChangeReport(IChangeReportBuilder.ChangesToString(changes), "", lighter =>
        {
            foreach (var cell in _perfect)
            {
                foreach (var p in snapshot.PossibilitiesAt(cell))
                {
                    lighter.HighlightPossibility(p, cell.Row, cell.Column, ChangeColoration.CauseOffTwo);
                }
            }
            foreach (var cell in _notPerfect)
            {
                foreach (var p in snapshot.PossibilitiesAt(cell))
                {
                    lighter.HighlightPossibility(p, cell.Row, cell.Column, _possibilities.Peek(p)
                    ? ChangeColoration.CauseOffTwo : ChangeColoration.CauseOnOne);
                }
            }

            IChangeReportBuilder.HighlightChanges(lighter, changes);
        });
    }
}