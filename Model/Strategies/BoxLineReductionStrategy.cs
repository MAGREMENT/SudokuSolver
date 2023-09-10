﻿using System.Collections.Generic;
using System.Linq;
using Model.Changes;
using Model.Positions;
using Model.Solver;
using Model.StrategiesUtil;

namespace Model.Strategies;

public class BoxLineReductionStrategy : IStrategy
{
    public string Name => "Box line reduction";
    public StrategyLevel Difficulty => StrategyLevel.Easy;
    public StatisticsTracker Tracker { get; } = new();
    public void ApplyOnce(IStrategyManager strategyManager)
    {
        for (int row = 0; row < 9; row++)
        {
            for (int number = 1; number <= 9; number++)
            {
                var ppir = strategyManager.RowPositions(row, number);
                if (ppir.AreAllInSameMiniGrid())
                {
                    int miniRow = row / 3;
                    int miniCol = ppir.First() / 3;

                    for (int r = 0; r < 3; r++)
                    {
                        for (int c = 0; c < 3; c++)
                        {
                            int realRow = miniRow * 3 + r;
                            int realCol = miniCol * 3 + c;

                            if (realRow != row) strategyManager.ChangeBuffer.AddPossibilityToRemove(number, realRow, realCol);
                        }
                    }
                    
                    strategyManager.ChangeBuffer.Push(this,
                        new BoxLineReductionReportBuilder(row, ppir, number, Unit.Row));
                }
            }
        }
        
        for (int col = 0; col < 9; col++)
        {
            for (int number = 1; number <= 9; number++)
            {
                var ppic = strategyManager.ColumnPositions(col, number);
                if (ppic.AreAllInSameMiniGrid())
                {
                    int miniRow = ppic.First() / 3;
                    int miniCol = col / 3;

                    for (int r = 0; r < 3; r++)
                    {
                        for (int c = 0; c < 3; c++)
                        {
                            int realRow = miniRow * 3 + r;
                            int realCol = miniCol * 3 + c;

                            if (realCol != col) strategyManager.ChangeBuffer.AddPossibilityToRemove(number, realRow, realCol);
                        }
                    }
                    
                    strategyManager.ChangeBuffer.Push(this, 
                        new BoxLineReductionReportBuilder(col, ppic, number, Unit.Column));
                }
            }
        }
    }
}

public class BoxLineReductionReportBuilder : IChangeReportBuilder
{
    private readonly int _unitNumber;
    private readonly LinePositions _linePos;
    private readonly int _number;
    private readonly Unit _unit;

    public BoxLineReductionReportBuilder(int unitNumber, LinePositions linePos, int number, Unit unit)
    {
        _unitNumber = unitNumber;
        _linePos = linePos;
        _number = number; 
        _unit = unit;
    }
    
    public ChangeReport Build(List<SolverChange> changes, IPossibilitiesHolder snapshot)
    {
        List<Cell> causes = new();
        switch (_unit)
        {
            case Unit.Row :
                foreach (var col in _linePos)
                {
                    causes.Add(new Cell(_unitNumber, col));
                }
                break;
            case Unit.Column :
                foreach (var row in _linePos)
                {
                    causes.Add(new Cell(row, _unitNumber));
                }
                break;
        }

        return new ChangeReport(IChangeReportBuilder.ChangesToString(changes), "", lighter =>
        {
            foreach (var coord in causes)
            {
                lighter.HighlightPossibility(_number, coord.Row, coord.Col, ChangeColoration.CauseOffOne);
            }

            IChangeReportBuilder.HighlightChanges(lighter, changes);
        });
    }
}