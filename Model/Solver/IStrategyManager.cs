﻿using System.Collections.Generic;
using Model.Changes;
using Model.Positions;
using Model.Possibilities;
using Model.StrategiesUtil;
using Model.StrategiesUtil.LinkGraph;

namespace Model.Solver;

public interface IStrategyManager : IPossibilitiesHolder
{
    bool AddSolution(int number, int row, int col, IStrategy strategy);

    bool RemovePossibility(int possibility, int row, int col, IStrategy strategy);

    ChangeBuffer ChangeBuffer { get; }

    public List<AlmostLockedSet> AlmostLockedSets();
    
    public LinkGraph<ILinkGraphElement> LinkGraph();

    public Dictionary<ILinkGraphElement, Coloring> OnColoring(int row, int col, int possibility);

    public Dictionary<ILinkGraphElement, Coloring> OffColoring(int row, int col, int possibility);

    public IReadOnlySudoku Sudoku { get; }

    public IPossibilities[,] Possibilities { get; } //Eventually remove this

    public Solver Copy();
}




