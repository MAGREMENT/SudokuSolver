﻿using Model.Solver.StrategiesUtility;
using Model.Solver.StrategiesUtility.Graphs;

namespace Model.Solver.Strategies.BlossomLoops;

public interface IBlossomLoopBranchFinder
{
    BlossomLoopBranch[]? FindShortestBranches(ILinkGraph<IChainingElement> graph,
        CellPossibility[] cps, LinkGraphLoop<IChainingElement> loop);
}