﻿using System.Collections.Generic;
using System.Linq;
using Model.Positions;

namespace Model.Strategies;

public class PointingPossibilitiesStrategy : IStrategy
{
    public string Name { get; } = "Pointing possibilities";
    
    public StrategyLevel Difficulty { get; } = StrategyLevel.Easy;
    public int Score { get; set; }

    public void ApplyOnce(IStrategyManager strategyManager)
    {
        for (int miniRow = 0; miniRow < 3; miniRow++)
        {
            for (int miniCol = 0; miniCol < 3; miniCol++)
            {
                for (int number = 1; number <= 9; number++)
                {
                    var ppimg = strategyManager.PossibilityPositionsInMiniGrid(miniRow, miniCol, number);
                    if (ppimg.AreAllInSameRow())
                    {
                        var changeBuffer = strategyManager.GetChangeBuffer();
                        int row = ppimg.First()[0];
                        for (int col = 0; col < 9; col++)
                        {
                            if (col / 3 != miniCol) changeBuffer.AddPossibilityToRemove(number, row, col);
                        }
                        
                        changeBuffer.Push(this,
                            new PointingPossibilitiesReportBuilder(number, ppimg));
                    }
                    else if (ppimg.AreAllInSameColumn())
                    {
                        var changeBuffer = strategyManager.GetChangeBuffer();
                        int col = ppimg.First()[1];
                        for (int row = 0; row < 9; row++)
                        {
                            if (row / 3 != miniRow) changeBuffer.AddPossibilityToRemove(number, row, col);
                        }
                        
                        changeBuffer.Push(this,
                            new PointingPossibilitiesReportBuilder(number, ppimg));
                    }
                }
            }
        }
    }
}

public class PointingPossibilitiesReportBuilder : IChangeReportBuilder
{
    private readonly int _number;
    private readonly MiniGridPositions _miniPos;

    public PointingPossibilitiesReportBuilder(int number, MiniGridPositions miniPos)
    {
        _number = number;
        _miniPos = miniPos;
    }
    
    public ChangeReport Build(List<SolverChange> changes, IChangeManager manager)
    {
        return new ChangeReport(IChangeReportBuilder.ChangesToString(changes), lighter =>
        {
            foreach (var pos in _miniPos)
            {
                lighter.HighlightPossibility(_number, pos[0], pos[1], ChangeColoration.CauseOffOne);
            }
            
            IChangeReportBuilder.HighlightChanges(lighter, changes);
        }, "");
    }
}