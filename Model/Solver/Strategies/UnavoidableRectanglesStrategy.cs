using System.Collections.Generic;
using Global;
using Global.Enums;
using Model.Solver.Helpers.Changes;
using Model.Solver.PossibilityPosition;
using Model.Solver.StrategiesUtility;

namespace Model.Solver.Strategies;

public class UnavoidableRectanglesStrategy : AbstractStrategy
{
    public const string OfficialName = "Unavoidable Rectangles";
    private const OnCommitBehavior DefaultBehavior = OnCommitBehavior.Return;
    
    public override OnCommitBehavior DefaultOnCommitBehavior => DefaultBehavior;

    public UnavoidableRectanglesStrategy() : base(OfficialName, StrategyDifficulty.Hard, DefaultBehavior)
    {
        UniquenessDependency = UniquenessDependency.FullyDependent;
    }
    
    public override void Apply(IStrategyManager strategyManager)
    {
        for (int i = 0; i < 81; i++)
        {
            var row1 = i / 9;
            var col1 = i % 9;

            if (strategyManager.Sudoku[row1, col1] == 0 || strategyManager.StartState[row1, col1] != 0) continue;
            
            for (int j = i + 1; j < 81; j++)
            {
                var row2 = j / 9;
                var col2 = j % 9;

                if (strategyManager.Sudoku[row2, col2] == 0 || strategyManager.StartState[row2, col2] != 0) continue;

                if (Search(strategyManager, new BiValue(strategyManager.Sudoku[row1, col1],
                        strategyManager.Sudoku[row2, col2]), new Cell(row1, col1), new Cell(row2, col2))) return;
            }
        }
    }

    private bool Search(IStrategyManager strategyManager, BiValue values, params Cell[] floor)
    {
        foreach (var roof in Cells.DeadlyPatternRoofs(floor))
        {
            if (Try(strategyManager, values, floor, roof)) return true;
        }
        
        return false;
    }

    private bool Try(IStrategyManager strategyManager, BiValue values, Cell[] floor, Cell[] roof)
    {
        if (strategyManager.StartState[roof[0].Row, roof[0].Column] != 0 || strategyManager.StartState[roof[1].Row, roof[1].Column] != 0) return false;
        
        var solved1 = strategyManager.Sudoku[roof[0].Row, roof[0].Column];
        var solved2 = strategyManager.Sudoku[roof[1].Row, roof[1].Column];
        
        switch (solved1, solved2)
        {
            case (not 0, not 0) :
                return false;
            case (0, not 0) :
                if (solved2 == values.One)
                {
                   strategyManager.ChangeBuffer.ProposePossibilityRemoval(values.Two, roof[0]);
                   return strategyManager.ChangeBuffer.NotEmpty() && strategyManager.ChangeBuffer.Commit(this,
                              new AvoidableRectanglesReportBuilder(floor, roof)) && OnCommitBehavior == OnCommitBehavior.Return;
                }

                return false;
            case(not 0, 0) :
                if (solved1 == values.Two)
                {
                    strategyManager.ChangeBuffer.ProposePossibilityRemoval(values.One, roof[1]);
                    return strategyManager.ChangeBuffer.NotEmpty() && strategyManager.ChangeBuffer.Commit(this,
                        new AvoidableRectanglesReportBuilder(floor, roof)) && OnCommitBehavior == OnCommitBehavior.Return;
                }
                
                return false;
        }

        var possibilitiesRoofOne = strategyManager.PossibilitiesAt(roof[0]);
        var possibilitiesRoofTwo = strategyManager.PossibilitiesAt(roof[1]);

        if (!possibilitiesRoofOne.Peek(values.Two) || !possibilitiesRoofTwo.Peek(values.One)) return false;

        if (possibilitiesRoofOne.Count == 2 && possibilitiesRoofTwo.Count == 2)
        {
            var and = possibilitiesRoofOne.And(possibilitiesRoofTwo);
            if (and.Count == 1)
            {
                var possibility = and.First();
                foreach (var cell in Cells.SharedSeenCells(roof[0], roof[1]))
                {
                    strategyManager.ChangeBuffer.ProposePossibilityRemoval(possibility, cell);
                }
            }
        }

        if (strategyManager.ChangeBuffer.NotEmpty() && strategyManager.ChangeBuffer.Commit(this,
                new AvoidableRectanglesReportBuilder(floor, roof)) && OnCommitBehavior == OnCommitBehavior.Return) return true;

        var notBiValuePossibilities = possibilitiesRoofOne.Or(possibilitiesRoofTwo);
        notBiValuePossibilities.Remove(values.One);
        notBiValuePossibilities.Remove(values.Two);
        var ssc = new List<Cell>(Cells.SharedSeenCells(roof[0], roof[1]));
        foreach (var als in strategyManager.AlmostNakedSetSearcher.InCells(ssc))
        {
            if (!als.Possibilities.PeekAll(notBiValuePossibilities)) continue;

            ProcessArWithAls(strategyManager, roof, als);
            if (strategyManager.ChangeBuffer.NotEmpty() && strategyManager.ChangeBuffer.Commit(this,
                    new AvoidableRectanglesWithAlmostLockedSetReportBuilder(floor, roof, als)) &&
                        OnCommitBehavior == OnCommitBehavior.Return) return true;
        }

        return false;
    }
    
    private void ProcessArWithAls(IStrategyManager strategyManager, Cell[] roof, IPossibilitiesPositions als)
    {
        List<Cell> buffer = new();
        foreach (var possibility in als.Possibilities)
        {
            foreach (var cell in als.EachCell())
            {
                if(strategyManager.PossibilitiesAt(cell).Peek(possibility)) buffer.Add(cell);
            }

            foreach (var r in roof)
            {
                if (strategyManager.PossibilitiesAt(r).Peek(possibility)) buffer.Add(r);
            }

            foreach (var cell in Cells.SharedSeenCells(buffer))
            {
                strategyManager.ChangeBuffer.ProposePossibilityRemoval(possibility, cell);
            }
            
            buffer.Clear();
        }
    }
}

public class AvoidableRectanglesReportBuilder : IChangeReportBuilder
{
    private readonly Cell[] _floor;
    private readonly Cell[] _roof;

    public AvoidableRectanglesReportBuilder(Cell[] floor, Cell[] roof)
    {
        _floor = floor;
        _roof = roof;
    }

    public ChangeReport Build(List<SolverChange> changes, IPossibilitiesHolder snapshot)
    {
        return new ChangeReport(IChangeReportBuilder.ChangesToString(changes), "", lighter =>
        {
            foreach (var floor in _floor)
            {
                lighter.HighlightCell(floor, ChangeColoration.CauseOffTwo);
            }

            foreach (var roof in _roof)
            {
                lighter.HighlightCell(roof, snapshot.Sudoku[roof.Row, roof.Column] == 0 ? ChangeColoration.CauseOffOne
                    : ChangeColoration.CauseOffTwo);
            }

            IChangeReportBuilder.HighlightChanges(lighter, changes);
        });
    }
}

public class AvoidableRectanglesWithAlmostLockedSetReportBuilder : IChangeReportBuilder
{
    private readonly Cell[] _floor;
    private readonly Cell[] _roof;
    private readonly IPossibilitiesPositions _als;

    public AvoidableRectanglesWithAlmostLockedSetReportBuilder(Cell[] floor, Cell[] roof, IPossibilitiesPositions als)
    {
        _floor = floor;
        _roof = roof;
        _als = als;
    }

    public ChangeReport Build(List<SolverChange> changes, IPossibilitiesHolder snapshot)
    {
        return new ChangeReport(IChangeReportBuilder.ChangesToString(changes), "", lighter =>
        {
            foreach (var floor in _floor)
            {
                lighter.HighlightCell(floor, ChangeColoration.CauseOffTwo);
            }

            foreach (var roof in _roof)
            {
                lighter.HighlightCell(roof, ChangeColoration.CauseOffOne);
            }

            foreach (var cell in _als.EachCell())
            {
                lighter.HighlightCell(cell, ChangeColoration.CauseOffThree);
            }
            
            IChangeReportBuilder.HighlightChanges(lighter, changes);
        });
    }
}