﻿using System;
using System.Collections.Generic;
using Global;
using Global.Enums;
using Model.Solver.Helpers.Changes;
using Model.Solver.Helpers.Highlighting;
using Model.Solver.Position;
using Model.Solver.Possibility;

namespace Model.Solver.Strategies;

public class NakedSetStrategy : AbstractStrategy
{
    public const string OfficialNameForType2 = "Naked Double";
    public const string OfficialNameForType3 = "Naked Triple";
    public const string OfficialNameForType4 = "Naked Quad";
    private const OnCommitBehavior DefaultBehavior = OnCommitBehavior.WaitForAll;
    
    public override OnCommitBehavior DefaultOnCommitBehavior => DefaultBehavior;

    private readonly int _type;

    public NakedSetStrategy(int type) : base("", StrategyDifficulty.None, DefaultBehavior)
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
    
    
    public override void Apply(IStrategyManager strategyManager)
    {
        for (int row = 0; row < 9; row++)
        {
            var possibleCols = EveryRowCellWithLessPossibilities(strategyManager, row, _type + 1);
            if (RecursiveRowMashing(strategyManager, Possibilities.NewEmpty(), possibleCols, -1, row,
                    new LinePositions())) return;
        }
        
        for (int col = 0; col < 9; col++)
        {
            var possibleRows = EveryColumnCellWithLessPossibilities(strategyManager, col, _type + 1);
            if (RecursiveColumnMashing(strategyManager, Possibilities.NewEmpty(), possibleRows, -1, col,
                    new LinePositions())) return;
        }
        
        for (int miniRow = 0; miniRow < 3; miniRow++)
        {
            for (int miniCol = 0; miniCol < 3; miniCol++)
            {
                var possibleGridNumbers = EveryMiniGridCellWithLessPossibilities(strategyManager, miniRow, miniCol, _type + 1);
                if (RecursiveMiniGridMashing(strategyManager, Possibilities.NewEmpty(), possibleGridNumbers, -1,
                        miniRow, miniCol, new MiniGridPositions(miniRow, miniCol))) return;
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

    private bool RecursiveRowMashing(IStrategyManager strategyManager, Possibilities current,
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
            {
                if (RemovePossibilitiesFromRow(strategyManager, row, newCurrent, newVisited)) return true;
            }
               
            else if (newVisited.Count < _type)
            {
                if (RecursiveRowMashing(strategyManager, newCurrent, possibleCols, cursor, row, newVisited))
                    return true;
            }
        }

        return false;
    }

    private bool RemovePossibilitiesFromRow(IStrategyManager strategyManager, int row, Possibilities toRemove, LinePositions except)
    {
        foreach (var n in toRemove)
        {
            for (int col = 0; col < 9; col++)
            {
                if (!except.Peek(col)) strategyManager.ChangeBuffer.ProposePossibilityRemoval(n, row, col);
            }
        }
        
        return strategyManager.ChangeBuffer.Commit(this, new LineNakedPossibilitiesReportBuilder(toRemove,
            except, row, Unit.Row)) && OnCommitBehavior == OnCommitBehavior.Return;
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
    
    private bool RecursiveColumnMashing(IStrategyManager strategyManager, Possibilities current,
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
            {
                if (RemovePossibilitiesFromColumn(strategyManager, col, newCurrent, newVisited)) return true;
            }
            else if (newVisited.Count < _type)
            {
                if (RecursiveColumnMashing(strategyManager, newCurrent, possibleRows, cursor, col, newVisited))
                    return true;
            }
        }

        return false;
    }

    private bool RemovePossibilitiesFromColumn(IStrategyManager strategyManager, int col, Possibilities toRemove, LinePositions except)
    {
        foreach (var n in toRemove)
        {
            for (int row = 0; row < 9; row++)
            {
                if (!except.Peek(row)) strategyManager.ChangeBuffer.ProposePossibilityRemoval(n, row, col);
            }
        }
        
        return strategyManager.ChangeBuffer.Commit(this, new LineNakedPossibilitiesReportBuilder(toRemove, except,
            col, Unit.Column)) && OnCommitBehavior == OnCommitBehavior.Return;
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
    
    private bool RecursiveMiniGridMashing(IStrategyManager strategyManager, Possibilities current,
        MiniGridPositions possiblePos, int cursor, int miniRow, int miniCol, MiniGridPositions visited)
    {
        Cell pos;
        while((pos = possiblePos.Next(ref cursor)).Row != -1)
        {
            var possibilities = strategyManager.PossibilitiesAt(pos.Row, pos.Column);
            if(possibilities.Count > _type) continue;
            
            var newCurrent = current.Or(possibilities);
            if (newCurrent.Count > _type) continue;
            
            var newVisited = visited.Copy();
            newVisited.Add(pos.Row % 3, pos.Column % 3);

            if (newVisited.Count == _type && newCurrent.Count == _type)
            {
                if (RemovePossibilitiesFromMiniGrid(strategyManager, miniRow, miniCol, newCurrent, newVisited))
                    return true;
            }
            else if (newVisited.Count < _type)
            {
                if (RecursiveMiniGridMashing(strategyManager, newCurrent, possiblePos, cursor, miniRow, miniCol,
                        newVisited)) return true;
            }
        }

        return false;
    }
    
    private bool RemovePossibilitiesFromMiniGrid(IStrategyManager strategyManager, int miniRow, int miniCol, Possibilities toRemove,
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
                
                    if (!except.Peek(gridRow, gridCol)) strategyManager.ChangeBuffer.ProposePossibilityRemoval(n, row, col);
                }
            }
        }
        
        return strategyManager.ChangeBuffer.Commit(this, new MiniGridNakedPossibilitiesReportBuilder(toRemove,
            except)) && OnCommitBehavior == OnCommitBehavior.Return;
    }
}

public class LineNakedPossibilitiesReportBuilder : IChangeReportBuilder
{
    private readonly Possibilities _possibilities;
    private readonly LinePositions _linePos;
    private readonly int _unitNumber;
    private readonly Unit _unit;


    public LineNakedPossibilitiesReportBuilder(Possibilities possibilities, LinePositions linePos, int unitNumber, Unit unit)
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
    private readonly Possibilities _possibilities;
    private readonly MiniGridPositions _miniPos;

    public MiniGridNakedPossibilitiesReportBuilder(Possibilities possibilities, MiniGridPositions miniPos)
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
                    if(snapshot.PossibilitiesAt(pos.Row, pos.Column).Peek(possibility))
                        lighter.HighlightPossibility(possibility, pos.Row, pos.Column, ChangeColoration.CauseOffOne);
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