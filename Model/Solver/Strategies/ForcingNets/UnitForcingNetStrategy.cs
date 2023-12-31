﻿using System.Collections.Generic;
using Model.Solver.Helpers.Changes;
using Model.Solver.Helpers.Highlighting;
using Model.Solver.StrategiesUtility;
using Model.Solver.StrategiesUtility.CellColoring;
using Model.Solver.StrategiesUtility.CellColoring.ColoringResults;
using Model.Solver.StrategiesUtility.Graphs;

namespace Model.Solver.Strategies.ForcingNets;

public class UnitForcingNetStrategy : AbstractStrategy
{
    public const string OfficialName = "Unit Forcing Net";
    private const OnCommitBehavior DefaultBehavior = OnCommitBehavior.Return;
    
    public override OnCommitBehavior DefaultOnCommitBehavior => DefaultBehavior;
    
    private readonly int _max;

    public UnitForcingNetStrategy(int maxPossibilities) : base(OfficialName, StrategyDifficulty.Extreme, DefaultBehavior)
    {
        _max = maxPossibilities;
    }
    
    public override void Apply(IStrategyManager strategyManager)
    {
        for (int number = 1; number <= 9; number++)
        {
            for (int row = 0; row < 9; row++)
            {
                var ppir = strategyManager.RowPositionsAt(row, number);
                if (ppir.Count < 2 || ppir.Count > _max) continue;
                
                var colorings = new ColoringDictionary<ILinkGraphElement>[ppir.Count];

                var cursor = 0;
                foreach (var col in ppir)
                {
                    colorings[cursor] = strategyManager.PreComputer.OnColoring(row, col, number);
                    cursor++;
                }

                if (Process(strategyManager, colorings)) return;
            }

            for (int col = 0; col < 9; col++)
            {
                var ppic = strategyManager.ColumnPositionsAt(col, number);
                if (ppic.Count < 2 || ppic.Count > _max) continue;
                
                var colorings = new ColoringDictionary<ILinkGraphElement>[ppic.Count];

                var cursor = 0;
                foreach (var row in ppic)
                {
                    colorings[cursor] = strategyManager.PreComputer.OnColoring(row, col, number);
                    cursor++;
                }

                if (Process(strategyManager, colorings)) return;
            }

            for (int miniRow = 0; miniRow < 3; miniRow++)
            {
                for (int miniCol = 0; miniCol < 3; miniCol++)
                {
                    var ppimn = strategyManager.MiniGridPositionsAt(miniRow, miniCol, number);
                    if (ppimn.Count < 2 || ppimn.Count > _max) continue;
                
                    var colorings = new ColoringDictionary<ILinkGraphElement>[ppimn.Count];

                    var cursor = 0;
                    foreach (var pos in ppimn)
                    {
                        colorings[cursor] = strategyManager.PreComputer.OnColoring(pos.Row, pos.Column, number);
                        cursor++;
                    }

                    if (Process(strategyManager, colorings)) return;
                }
            }
        }
    }

    private bool Process(IStrategyManager view, ColoringDictionary<ILinkGraphElement>[] colorings)
    {
        foreach (var element in colorings[0])
        {
            if (element.Key is not CellPossibility current) continue;

            bool sameInAll = true;
            Coloring col = element.Value;

            for (int i = 1; i < colorings.Length && sameInAll; i++)
            {
                if (!colorings[i].TryGetValue(current, out var c) || c != col)
                {
                    sameInAll = false;
                    break;
                }
            }

            if (sameInAll)
            {
                if (col == Coloring.On)
                {
                    view.ChangeBuffer.ProposeSolutionAddition(current.Possibility, current.Row, current.Column);
                    if (view.ChangeBuffer.NotEmpty() && view.ChangeBuffer.Commit(this,
                            new UnitForcingNetReportBuilder(colorings, current, Coloring.On, view.GraphManager.ComplexLinkGraph)) &&
                                OnCommitBehavior == OnCommitBehavior.Return) return true;
                }
                else
                {
                    view.ChangeBuffer.ProposePossibilityRemoval(current.Possibility, current.Row, current.Column);
                    if (view.ChangeBuffer.NotEmpty() && view.ChangeBuffer.Commit(this,
                            new UnitForcingNetReportBuilder(colorings, current, Coloring.Off, view.GraphManager.ComplexLinkGraph)) &&
                                OnCommitBehavior == OnCommitBehavior.Return) return true;
                }
            }
        }

        return false;
    }
}

public class UnitForcingNetReportBuilder : IChangeReportBuilder
{
    private readonly ColoringDictionary<ILinkGraphElement>[] _colorings;
    private readonly CellPossibility _target;
    private readonly Coloring _targetColoring;
    private readonly LinkGraph<ILinkGraphElement> _graph;

    public UnitForcingNetReportBuilder(ColoringDictionary<ILinkGraphElement>[] colorings, CellPossibility target, Coloring targetColoring, LinkGraph<ILinkGraphElement> graph)
    {
        _colorings = colorings;
        _target = target;
        _targetColoring = targetColoring;
        _graph = graph;
    }

    public ChangeReport Build(List<SolverChange> changes, IPossibilitiesHolder snapshot)
    {
        Highlight[] highlights = new Highlight[_colorings.Length];
        var paths = new List<LinkGraphChain<ILinkGraphElement>>[_colorings.Length];

        for (int i = 0; i < _colorings.Length; i++)
        {
            paths[i] = ForcingNetsUtility.FindEveryNeededPaths(_colorings[i].History!.GetPathToRootWithGuessedLinks(_target,
                _targetColoring), _colorings[i], _graph, snapshot);
            
            var iForDelegate = i;
            highlights[i] = lighter =>
            {
                ForcingNetsUtility.HighlightAllPaths(lighter, paths[iForDelegate], Coloring.On);
                
                if (paths[iForDelegate][0].Elements[0] is CellPossibility start) lighter.EncirclePossibility(start);
                IChangeReportBuilder.HighlightChanges(lighter, changes);
            };
        }
        
        return new ChangeReport(IChangeReportBuilder.ChangesToString(changes), "", highlights);
    }
}