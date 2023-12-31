﻿using System;
using System.Collections.Generic;
using Model.Solver.Helpers;

namespace Model.Solver;

public abstract class AbstractStrategy : IStrategy
{ 
    public string Name { get; protected init; }
    public StrategyDifficulty Difficulty { get; protected init; }
    public UniquenessDependency UniquenessDependency { get; protected init; }
    public OnCommitBehavior OnCommitBehavior { get; set; }
    public abstract OnCommitBehavior DefaultOnCommitBehavior { get; }
    public StatisticsTracker Tracker { get; } = new();
    public IReadOnlyList<IStrategyArgument> Arguments => ArgumentsList.ToArray();

    protected List<IStrategyArgument> ArgumentsList { get; } = new();

    protected AbstractStrategy(string name, StrategyDifficulty difficulty, OnCommitBehavior defaultBehavior)
    {
        Name = name;
        Difficulty = difficulty;
        UniquenessDependency = UniquenessDependency.NotDependent;
        OnCommitBehavior = defaultBehavior;
    }
    
    public abstract void Apply(IStrategyManager strategyManager);
    public virtual void OnNewSudoku(Sudoku s) { }
    public void TrySetArgument(string name, string value)
    {
        foreach (var arg in Arguments)
        {
            if (!arg.Name.Equals(name)) continue;

            arg.Set(value);
        }
    }
}