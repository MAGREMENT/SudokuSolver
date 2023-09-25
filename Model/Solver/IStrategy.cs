﻿using Model.Solver.Helpers;

namespace Model.Solver;

public interface IStrategy //TODO : Add return after first instance found
{
    public string Name { get; }
    public StrategyDifficulty Difficulty { get; }
    public StatisticsTracker Tracker { get; }

    void ApplyOnce(IStrategyManager strategyManager);
}

public enum StrategyDifficulty
{
    None, Basic, Easy, Medium, Hard, Extreme, ByTrial
}