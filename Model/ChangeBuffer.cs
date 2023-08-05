﻿using System.Collections.Generic;
using Model.Logs;
using Model.Positions;
using Model.Possibilities;
using Model.StrategiesUtil;

namespace Model;

public class ChangeBuffer
{
    private List<int>? _possibilityRemoved;
    private List<int>? _definitiveAdded;

    private readonly IChangeManager _m;
    private readonly IChangeReport _report;
    private readonly IStrategy _strategy;

    public ChangeBuffer(IChangeManager changeManager, IStrategy strategy, IChangeReport report)
    {
        _m = changeManager;
        _report = report;
        _strategy = strategy;
    }

    public void AddPossibilityToRemove(int possibility, int row, int col)
    {
        if (!_m.Possibilities[row, col].Peek(possibility)) return;
        _possibilityRemoved ??= new List<int>();
        _possibilityRemoved.Add(possibility * 81 + row * 9 + col);
    }

    public void AddDefinitiveToAdd(int number, int row, int col)
    {
        _definitiveAdded ??= new List<int>();
        _definitiveAdded.Add(number * 81 + row * 9 + col);
    }

    public bool Push()
    {
        List<LogChange> changes = new();
        if (_possibilityRemoved is not null)
        {
            foreach (var possibility in _possibilityRemoved)
            {
                int n = possibility % 81;
                if (_m.RemovePossibility(possibility / 81, n / 9, n % 9)) changes.Add(
                    new LogChange(SolverNumberType.Possibility, possibility / 81, n / 9, n % 9));
            
            } 
        }

        if (_definitiveAdded is not null)
        {
            foreach (var definitive in _definitiveAdded)
            {
                int n = definitive % 81;
                if(_m.AddDefinitive(definitive / 81, n / 9, n % 9)) changes.Add(
                    new LogChange(SolverNumberType.Definitive, definitive / 81, n / 9, n % 9));
            }  
        }

        if (_m.LogsManaged && changes.Count > 0)
        {
            _report.Process();
            _m.PushLog(_report, _strategy);
        }

        return changes.Count > 0;
    }
}