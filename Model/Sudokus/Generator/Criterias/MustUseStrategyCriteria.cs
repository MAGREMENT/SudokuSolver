﻿using System;
using System.Collections.Generic;
using Model.Helpers.Settings;
using Model.Helpers.Settings.Types;
using Model.Sudokus.Solver.Strategies;
using Model.Sudokus.Solver.Trackers;

namespace Model.Sudokus.Generator.Criterias;

public class MustUseStrategyCriteria : EvaluationCriteria
{
    public const string OfficialName = "Must Use Strategy";
    
    public MustUseStrategyCriteria(IReadOnlyList<string> usedStrategies) : base(OfficialName, 
        new StringSetting("StrategyName", new AutoFillingInteractionInterface(usedStrategies),
            NakedSingleStrategy.OfficialName))
    {
    }

    public override bool IsValid(GeneratedSudokuPuzzle puzzle, UsedStrategiesTracker usedStrategiesTracker)
    {
        return usedStrategiesTracker.WasUsed(_settings[0].ToString()!);
    }

    public override bool Equals(object? obj)
    {
        return obj is MustUseStrategyCriteria criteria &&
               criteria.Settings[0].Get().Equals(Settings[0].Get());
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, _settings[0].Get().GetHashCode());
    }
}