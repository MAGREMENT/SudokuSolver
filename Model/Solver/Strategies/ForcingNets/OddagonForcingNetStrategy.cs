﻿using System.Collections.Generic;
using Global;
using Global.Enums;
using Model.Solver.Helpers.Changes;
using Model.Solver.Helpers.Highlighting;
using Model.Solver.StrategiesUtility;
using Model.Solver.StrategiesUtility.CellColoring;
using Model.Solver.StrategiesUtility.CellColoring.ColoringResults;
using Model.Solver.StrategiesUtility.Graphs;
using Model.Solver.StrategiesUtility.Oddagons;

namespace Model.Solver.Strategies.ForcingNets;

public class OddagonForcingNetStrategy : AbstractStrategy
{
    public const string OfficialName = "Oddagon Forcing Net";
    private const OnCommitBehavior DefaultBehavior = OnCommitBehavior.Return;

    public override OnCommitBehavior DefaultOnCommitBehavior => DefaultBehavior;

    private int _maxNumberOfGuardians;
    
    public OddagonForcingNetStrategy(int maxNumberOfGuardians) : base(OfficialName, StrategyDifficulty.Extreme, DefaultBehavior)
    {
        _maxNumberOfGuardians = maxNumberOfGuardians;
        ArgumentsList.Add(new IntStrategyArgument("Maximum number of guardians", () => _maxNumberOfGuardians,
            i => _maxNumberOfGuardians = i, new SliderViewInterface(1, 20, 1)));
    }
    
    public override void Apply(IStrategyManager strategyManager)
    {
        foreach (var oddagon in strategyManager.PreComputer.AlmostOddagons())
        {
            if (oddagon.Guardians.Length > _maxNumberOfGuardians) continue;

            var colorings = new ColoringDictionary<ILinkGraphElement>[oddagon.Guardians.Length];
            for (int i = 0; i < oddagon.Guardians.Length; i++)
            {
                var current = oddagon.Guardians[i];
                colorings[i] = strategyManager.PreComputer.OnColoring(current.Row, current.Column, current.Possibility);
            }

            foreach (var element in colorings[0])
            {
                if (element.Key is not CellPossibility cp) continue;
                
                bool ok = true;
                
                for (int i = 1; i < colorings.Length; i++)
                {
                    if (!colorings[i].TryGetColoredElement(element.Key, out var c) || c != element.Value)
                    {
                        ok = false;
                        break;
                    }
                }

                if (ok)
                {
                    if (element.Value == Coloring.On) strategyManager.ChangeBuffer.ProposeSolutionAddition(cp);
                    else strategyManager.ChangeBuffer.ProposePossibilityRemoval(cp);

                    if (strategyManager.ChangeBuffer.NotEmpty() && strategyManager.ChangeBuffer.Commit(this, 
                            new OddagonForcingNetReportBuilder(colorings, element.Value, oddagon,
                                strategyManager.GraphManager.ComplexLinkGraph, cp)) && OnCommitBehavior == OnCommitBehavior.Return) return;
                }
            }
        }
    }
}

public class OddagonForcingNetReportBuilder : IChangeReportBuilder
{
    private readonly ColoringDictionary<ILinkGraphElement>[] _colorings;
    private readonly Coloring _changeColoring;
    private readonly CellPossibility _change;
    private readonly AlmostOddagon _oddagon;
    private readonly LinkGraph<ILinkGraphElement> _graph;

    public OddagonForcingNetReportBuilder(ColoringDictionary<ILinkGraphElement>[] colorings, Coloring changeColoring, AlmostOddagon oddagon, LinkGraph<ILinkGraphElement> graph, CellPossibility change)
    {
        _colorings = colorings;
        _changeColoring = changeColoring;
        _oddagon = oddagon;
        _graph = graph;
        _change = change;
    }

    public ChangeReport Build(List<SolverChange> changes, IPossibilitiesHolder snapshot)
    {
        var highlights = new Highlight[_colorings.Length];
        for (int i = 0; i < _colorings.Length; i++)
        {
            var iForDelegate = i;
            highlights[i] = lighter =>
            {
                var paths = ForcingNetsUtility.FindEveryNeededPaths(_colorings[iForDelegate].History!.GetPathToRootWithGuessedLinks(_change,
                    _changeColoring), _colorings[iForDelegate], _graph, snapshot);

                ForcingNetsUtility.HighlightAllPaths(lighter, paths, Coloring.On);
                lighter.EncirclePossibility(_oddagon.Guardians[iForDelegate]);
                IChangeReportBuilder.HighlightChanges(lighter, changes);
            };
        }

        return new ChangeReport(IChangeReportBuilder.ChangesToString(changes), "", lighter =>
        {
            foreach (var element in _oddagon.Loop.Elements)
            {
                lighter.HighlightPossibility(element, ChangeColoration.CauseOffTwo);
            }
            
            _oddagon.Loop.ForEachLink((one, two)
                => lighter.CreateLink(one, two, LinkStrength.Strong), LinkStrength.Strong);
            _oddagon.Loop.ForEachLink((one, two)
                => lighter.CreateLink(one, two, LinkStrength.Weak), LinkStrength.Weak);

            foreach (var cp in _oddagon.Guardians)
            {
                lighter.EncirclePossibility(cp);
                lighter.HighlightPossibility(cp, ChangeColoration.CauseOnOne);
            }

            IChangeReportBuilder.HighlightChanges(lighter, changes);
        }, highlights);
    }
}