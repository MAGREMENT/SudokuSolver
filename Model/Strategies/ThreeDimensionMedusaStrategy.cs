﻿using System.Collections.Generic;
using Model.Solver;
using Model.StrategiesUtil;
using Model.StrategiesUtil.LinkGraph;

namespace Model.Strategies;

public class ThreeDimensionMedusaStrategy : IStrategy
{
    public string Name => "3D Medusa";
    public StrategyLevel Difficulty => StrategyLevel.Hard;
    public StatisticsTracker Tracker { get; } = new();
    public void ApplyOnce(IStrategyManager strategyManager)
    {
        var manager = new LinkGraphManager(strategyManager);
        manager.Construct(ConstructRule.UnitStrongLink, ConstructRule.CellStrongLink);
        var graph = manager.LinkGraph;

        foreach (var coloredVertices in ColorHelper.Color<PossibilityCoordinate>(graph))
        {
            HashSet<PossibilityCoordinate> inGraph = new HashSet<PossibilityCoordinate>(coloredVertices.On);
            inGraph.UnionWith(coloredVertices.Off);
            
            if (!SearchColor(strategyManager, coloredVertices.On, coloredVertices.Off, inGraph) &&
                !SearchColor(strategyManager, coloredVertices.Off, coloredVertices.On, inGraph))
                SearchMix(strategyManager, coloredVertices.On, coloredVertices.Off, inGraph);

            if (strategyManager.ChangeBuffer.NotEmpty())
                strategyManager.ChangeBuffer.Push(this, new SimpleColoringReportBuilder(coloredVertices));
        }
    }

    private bool SearchColor(IStrategyManager strategyManager, List<PossibilityCoordinate> toSearch,
        List<PossibilityCoordinate> other, HashSet<PossibilityCoordinate> inGraph)
    {
        for (int i = 0; i < toSearch.Count; i++)
        {
            for (int j = i + 1; j < toSearch.Count; j++)
            {
                var first = toSearch[i];
                var second = toSearch[j];

                bool sameCell = first.Row == second.Row && first.Col == second.Col;
                bool sameUnitAndPossibility = first.Possibility == second.Possibility && first.ShareAUnit(second);
                bool doEmptyCell = DoEmptyCell(strategyManager, first, second, inGraph);
                
                if (sameCell || sameUnitAndPossibility || doEmptyCell)
                {
                    foreach (var coord in other)
                    {
                        strategyManager.ChangeBuffer.AddDefinitiveToAdd(coord);
                    }

                    return true;
                }
            }
        }

        return false;
    }

    private bool DoEmptyCell(IStrategyManager strategyManager, PossibilityCoordinate one, PossibilityCoordinate two,
        HashSet<PossibilityCoordinate> inGraph)
    {
        if (one.Row == two.Row || one.Col == two.Col || one.Possibility == two.Possibility) return false;

        foreach (var coord in one.SharedSeenCells(two))
        {
            if(!IsTotallyOffGraph(strategyManager, coord, inGraph)) continue;
            
            var possibilities = strategyManager.Possibilities[coord.Row, coord.Col];
            if (possibilities.Count == 2 && possibilities.Peek(one.Possibility) &&
                possibilities.Peek(two.Possibility)) return true;
        }

        return false;
    }

    private bool IsTotallyOffGraph(IStrategyManager strategyManager, Coordinate coordinate, HashSet<PossibilityCoordinate> inGraph)
    {
        foreach (var possibility in strategyManager.Possibilities[coordinate.Row, coordinate.Col])
        {
            if (inGraph.Contains(new PossibilityCoordinate(coordinate.Row, coordinate.Col, possibility))) return false;
        }

        return true;
    }

    private void SearchMix(IStrategyManager strategyManager, List<PossibilityCoordinate> one,
        List<PossibilityCoordinate> two, HashSet<PossibilityCoordinate> inGraph)
    {
        foreach (var first in one)
        {
            foreach (var second in two)
            {
                if (first.Possibility == second.Possibility)
                {
                    if(first.Row == second.Row || first.Col == second.Col) continue;

                    foreach (var coord in first.SharedSeenCells(second))
                    {
                        var current = new PossibilityCoordinate(coord.Row, coord.Col, first.Possibility);
                        if(inGraph.Contains(current)) continue; 
                        strategyManager.ChangeBuffer.AddPossibilityToRemove(current);
                    }
                }
                else
                {
                    if (first.Row == second.Row && first.Col == second.Col)
                        RemoveAllExcept(strategyManager, first.Row, first.Col, first.Possibility, second.Possibility);
                    else if(first.ShareAUnit(second))
                    {
                        if(strategyManager.Possibilities[first.Row, first.Col].Peek(second.Possibility) &&
                           !inGraph.Contains(new PossibilityCoordinate(first.Row, first.Col, second.Possibility)))
                            strategyManager.ChangeBuffer.AddPossibilityToRemove(second.Possibility, first.Row, first.Col);
                        
                        if(strategyManager.Possibilities[second.Row, second.Col].Peek(first.Possibility) &&
                           !inGraph.Contains(new PossibilityCoordinate(second.Row, second.Col, first.Possibility)))
                            strategyManager.ChangeBuffer.AddPossibilityToRemove(first.Possibility, second.Row, second.Col);
                    }
                }
            }
        }
    }
    
    private void RemoveAllExcept(IStrategyManager strategyManager, int row, int col, int exceptOne, int exceptTwo)
    {
        for (int i = 1; i <= 9; i++)
        {
            if (i != exceptOne && i != exceptTwo)
            {
                strategyManager.ChangeBuffer.AddPossibilityToRemove(i, row, col);
            }
        }
    }

}