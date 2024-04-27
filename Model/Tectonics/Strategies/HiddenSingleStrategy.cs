﻿using Model.Helpers;
using Model.Helpers.Changes;
using Model.Helpers.Highlighting;
using Model.Sudokus.Solver;

namespace Model.Tectonics.Strategies;

public class HiddenSingleStrategy : TectonicStrategy
{
    public HiddenSingleStrategy() : base("Hidden Single", StrategyDifficulty.Basic, InstanceHandling.UnorderedAll)
    {
    }
    
    public override void Apply(ITectonicStrategyUser strategyUser)
    {
        for(int i = 0; i < strategyUser.Tectonic.Zones.Count; i++)
        {
            for (int n = 1; n <= strategyUser.Tectonic.Zones[i].Count; n++)
            {
                var poss = strategyUser.ZonePositionsFor(i, n);
                if (poss.Count != 1) continue;

                strategyUser.ChangeBuffer.ProposeSolutionAddition(n, strategyUser.Tectonic.Zones[i][poss.First(0, strategyUser.Tectonic.Zones[i].Count)]);
                strategyUser.ChangeBuffer.Commit(DefaultChangeReportBuilder<IUpdatableTectonicSolvingState, ITectonicHighlighter>.Instance);
            }
        }
    }
}