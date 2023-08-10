﻿using System.Collections.Generic;
using Model.StrategiesUtil;

namespace Model;

public class ChangeBuffer
{
    private readonly HashSet<PossibilityCoordinate> _possibilityRemoved = new();
    private readonly HashSet<PossibilityCoordinate> _definitiveAdded = new();

    private readonly IChangeManager _m;

    public ChangeBuffer(IChangeManager changeManager)
    {
        _m = changeManager;
    }

    public void AddPossibilityToRemove(int possibility, int row, int col)
    {
        if (!_m.Possibilities[row, col].Peek(possibility)) return;
        _possibilityRemoved.Add(new PossibilityCoordinate(row, col, possibility));
    }

    public void AddDefinitiveToAdd(int number, int row, int col)
    {
        _definitiveAdded.Add(new PossibilityCoordinate(row, col, number));
    }

    public bool NotEmpty()
    {
        return _possibilityRemoved.Count > 0 || _definitiveAdded.Count > 0;
    }

    public bool Push(IStrategy strategy, IChangeReportBuilder builder)
    {
        if (_possibilityRemoved.Count == 0 && _definitiveAdded.Count == 0) return false;
        
        List<SolverChange> changes = new();
        foreach (var possibility in _possibilityRemoved)
        {
            if (_m.RemovePossibility(possibility.Possibility, possibility.Row, possibility.Col)) changes.Add(
                new SolverChange(SolverNumberType.Possibility,
                    possibility.Possibility, possibility.Row, possibility.Col));
            
        }
        
        foreach (var definitive in _definitiveAdded)
        {
            if(_m.AddDefinitive(definitive.Possibility, definitive.Row, definitive.Col)) changes.Add(
                new SolverChange(SolverNumberType.Definitive, definitive.Possibility,
                    definitive.Row, definitive.Col));
        }

        if (_m.LogsManaged && changes.Count > 0)
        {
            _m.PushChangeReportLog(builder.Build(changes, _m), strategy);
        }

        _possibilityRemoved.Clear();
        _definitiveAdded.Clear();
        return changes.Count > 0;
    }
}