﻿using System;
using System.Collections.Generic;
using Model.Solver.Helpers.Changes;
using Model.Solver.Helpers.Highlighting;
using Model.Solver.Positions;
using Model.Solver.Possibilities;
using Model.Solver.StrategiesUtil;

namespace Model.Solver.Strategies;

public class NakedSetStrategy : AbstractStrategy
{
    public const string OfficialNameForType2 = "Naked Double";
    public const string OfficialNameForType3 = "Naked Triple";
    public const string OfficialNameForType4 = "Naked Quad";

    private readonly int _type;

    public NakedSetStrategy(int type) : base("", StrategyDifficulty.None)
    {
        _type = type;
        switch (type)
        {
            case 2 : Name = OfficialNameForType2;
                Difficulty = StrategyDifficulty.Easy;
                break;
            case 3 : Name = OfficialNameForType3;
                Difficulty = StrategyDifficulty.Easy;
                break;
            case 4 : Name = OfficialNameForType4;
                Difficulty = StrategyDifficulty.Easy;
                break;
            default : throw new ArgumentException("Type not valid");
        }
    }
    
    
    public override void ApplyOnce(IStrategyManager strategyManager)
    {
        for (int row = 0; row < 9; row++)
        {
            var possibleCols = EveryRowCellWithLessPossibilities(strategyManager, row, _type + 1);
            RecursiveRowMashing(strategyManager, IPossibilities.NewEmpty(), possibleCols, -1, row, new LinePositions());
        }
        
        for (int col = 0; col < 9; col++)
        {
            var possibleRows = EveryColumnCellWithLessPossibilities(strategyManager, col, _type + 1);
            RecursiveColumnMashing(strategyManager, IPossibilities.NewEmpty(), possibleRows, -1, col, new LinePositions());
        }
        
        for (int miniRow = 0; miniRow < 3; miniRow++)
        {
            for (int miniCol = 0; miniCol < 3; miniCol++)
            {
                var possibleGridNumbers = EveryMiniGridCellWithLessPossibilities(strategyManager, miniRow, miniCol, _type + 1);
                RecursiveMiniGridMashing(strategyManager, IPossibilities.NewEmpty(), possibleGridNumbers, -1, miniRow, miniCol,
                    new MiniGridPositions(miniRow, miniCol));
            }
        }
    }

    private LinePositions EveryRowCellWithLessPossibilities(IStrategyManager strategyManager, int row, int than)
    {
        LinePositions result = new();
        for (int col = 0; col < 9; col++)
        {
            if (strategyManager.Sudoku[row, col] == 0 && strategyManager.PossibilitiesAt(row, col).Count < than)
                result.Add(col);
        }

        return result;
    }

    private void RecursiveRowMashing(IStrategyManager strategyManager, IPossibilities current,
        LinePositions possibleCols, int cursor, int row, LinePositions visited)
    {
        int col;
        while ((col = possibleCols.Next(ref cursor)) != -1)
        {
            var possibilities = strategyManager.PossibilitiesAt(row, col);
            if(possibilities.Count > _type) continue;
            
            var newCurrent = current.Or(possibilities);
            if (newCurrent.Count > _type) continue;
            
            var newVisited = visited.Copy();
            newVisited.Add(col);
            
            if (newVisited.Count == _type && newCurrent.Count == _type)
                RemovePossibilitiesFromRow(strategyManager, row, newCurrent, newVisited);
            else if (newVisited.Count < _type)
                RecursiveRowMashing(strategyManager, newCurrent, possibleCols, cursor, row, newVisited);
            
        }
    }

    private void RemovePossibilitiesFromRow(IStrategyManager strategyManager, int row, IPossibilities toRemove, LinePositions except)
    {
        foreach (var n in toRemove)
        {
            for (int col = 0; col < 9; col++)
            {
                if (!except.Peek(col)) strategyManager.ChangeBuffer.AddPossibilityToRemove(n, row, col);
            }
        }
        
        strategyManager.ChangeBuffer.Push(this, new LineNakedPossibilitiesReportBuilder(toRemove, except, row, Unit.Row));
    }
    
    private LinePositions EveryColumnCellWithLessPossibilities(IStrategyManager strategyManager, int col, int than)
    {
        LinePositions result = new();
        for (int row = 0; row < 9; row++)
        {
            if (strategyManager.Sudoku[row, col] == 0 && strategyManager.PossibilitiesAt(row, col).Count < than) 
                result.Add(row);
        }

        return result;
    }
    
    private void RecursiveColumnMashing(IStrategyManager strategyManager, IPossibilities current,
        LinePositions possibleRows, int cursor, int col, LinePositions visited)
    {
        int row;
        while((row = possibleRows.Next(ref cursor)) != -1)
        {
            var possibilities = strategyManager.PossibilitiesAt(row, col);
            if(possibilities.Count > _type) continue;
            
            var newCurrent = current.Or(possibilities);
            if (newCurrent.Count > _type) continue;
            
            var newVisited = visited.Copy();
            newVisited.Add(row);

            if (newVisited.Count == _type && newCurrent.Count == _type)
                RemovePossibilitiesFromColumn(strategyManager, col, newCurrent, newVisited);
            else if (newVisited.Count < _type)
                RecursiveColumnMashing(strategyManager, newCurrent, possibleRows, cursor, col, newVisited);
        }
    }

    private void RemovePossibilitiesFromColumn(IStrategyManager strategyManager, int col, IPossibilities toRemove, LinePositions except)
    {
        foreach (var n in toRemove)
        {
            for (int row = 0; row < 9; row++)
            {
                if (!except.Peek(row)) strategyManager.ChangeBuffer.AddPossibilityToRemove(n, row, col);
            }
        }
        
        strategyManager.ChangeBuffer.Push(this, new LineNakedPossibilitiesReportBuilder(toRemove, except, col, Unit.Column));
    }
    
    private MiniGridPositions EveryMiniGridCellWithLessPossibilities(IStrategyManager strategyManager, int miniRow, int miniCol, int than)
    {
        MiniGridPositions result = new(miniRow, miniCol);
        for (int gridRow = 0; gridRow < 3; gridRow++)
        {
            for (int gridCol = 0; gridCol < 3; gridCol++)
            {
                int row = miniRow * 3 + gridRow;
                int col = miniCol * 3 + gridCol;
            
                if (strategyManager.Sudoku[row, col] == 0 && strategyManager.PossibilitiesAt(row, col).Count < than) 
                    result.Add(gridRow, gridCol);
            }
        }
        
        return result;
    }
    
    private void RecursiveMiniGridMashing(IStrategyManager strategyManager, IPossibilities current,
        MiniGridPositions possiblePos, int cursor, int miniRow, int miniCol, MiniGridPositions visited)
    {
        Cell pos;
        while((pos = possiblePos.Next(ref cursor)).Row != -1)
        {
            var possibilities = strategyManager.PossibilitiesAt(pos.Row, pos.Col);
            if(possibilities.Count > _type) continue;
            
            var newCurrent = current.Or(possibilities);
            if (newCurrent.Count > _type) continue;
            
            var newVisited = visited.Copy();
            newVisited.Add(pos.Row % 3, pos.Col % 3);
            
            if (newVisited.Count == _type && newCurrent.Count == _type)
                RemovePossibilitiesFromMiniGrid(strategyManager, miniRow, miniCol, newCurrent, newVisited);
            else if (newVisited.Count < _type)
                RecursiveMiniGridMashing(strategyManager, newCurrent, possiblePos, cursor, miniRow, miniCol, newVisited);
        }
    }
    
    private void RemovePossibilitiesFromMiniGrid(IStrategyManager strategyManager, int miniRow, int miniCol, IPossibilities toRemove,
        MiniGridPositions except)
    {
        foreach (var n in toRemove)
        {
            for (int gridRow = 0; gridRow < 3; gridRow++)
            {
                for (int gridCol = 0; gridCol < 3; gridCol++)
                {
                    int row = miniRow * 3 + gridRow;
                    int col = miniCol * 3 + gridCol;
                
                    if (!except.Peek(gridRow, gridCol)) strategyManager.ChangeBuffer.AddPossibilityToRemove(n, row, col);
                }
            }
        }
        
        strategyManager.ChangeBuffer.Push(this, new MiniGridNakedPossibilitiesReportBuilder(toRemove, except));
    }
}

public class LineNakedPossibilitiesReportBuilder : IChangeReportBuilder
{
    private readonly IPossibilities _possibilities;
    private readonly LinePositions _linePos;
    private readonly int _unitNumber;
    private readonly Unit _unit;


    public LineNakedPossibilitiesReportBuilder(IPossibilities possibilities, LinePositions linePos, int unitNumber, Unit unit)
    {
        _possibilities = possibilities;
        _linePos = linePos;
        _unitNumber = unitNumber;
        _unit = unit;
    }

    public ChangeReport Build(List<SolverChange> changes, IPossibilitiesHolder snapshot)
    {
        return new ChangeReport(IChangeReportBuilder.ChangesToString(changes), Explanation(), lighter =>
        {
            foreach (var other in _linePos)
            {
                foreach (var possibility in _possibilities)
                {
                    switch (_unit)
                    {
                        case Unit.Row :
                            if(snapshot.PossibilitiesAt(_unitNumber, other).Peek(possibility))
                                lighter.HighlightPossibility(possibility, _unitNumber, other, ChangeColoration.CauseOffOne);
                            break;
                        case Unit.Column :
                            if(snapshot.PossibilitiesAt(other, _unitNumber).Peek(possibility))
                                lighter.HighlightPossibility(possibility, other, _unitNumber, ChangeColoration.CauseOffOne);
                            break;
                    }
                }
            }

            IChangeReportBuilder.HighlightChanges(lighter, changes);
        });
    }

    private string Explanation()
    {
        return $"The cells {_linePos.ToString(_unit, _unitNumber)} only contains the possibilities ({_possibilities})." +
               $" Any other cell in {_unit.ToString().ToLower()} {_unitNumber + 1} cannot contain these possibilities";
    }
}

public class MiniGridNakedPossibilitiesReportBuilder : IChangeReportBuilder
{
    private readonly IPossibilities _possibilities;
    private readonly MiniGridPositions _miniPos;

    public MiniGridNakedPossibilitiesReportBuilder(IPossibilities possibilities, MiniGridPositions miniPos)
    {
        _possibilities = possibilities;
        _miniPos = miniPos;
    }
    
    public ChangeReport Build(List<SolverChange> changes, IPossibilitiesHolder snapshot)
    {
        return new ChangeReport(IChangeReportBuilder.ChangesToString(changes), Explanation(), lighter =>
        {
            foreach (var pos in _miniPos)
            {
                foreach (var possibility in _possibilities)
                {
                    if(snapshot.PossibilitiesAt(pos.Row, pos.Col).Peek(possibility))
                        lighter.HighlightPossibility(possibility, pos.Row, pos.Col, ChangeColoration.CauseOffOne);
                }
            }

            IChangeReportBuilder.HighlightChanges(lighter, changes);
        });
    }
    
    private string Explanation()
    {
        return $"The cells {_miniPos} only contains the possibilities ({_possibilities}). Any other cell in" +
               $" mini grid {_miniPos.MiniGridNumber() + 1} cannot contain these possibilities";
    }
}