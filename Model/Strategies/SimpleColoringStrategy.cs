﻿using System.Collections.Generic;
using Model.StrategiesUtil;

namespace Model.Strategies;

public class SimpleColoringStrategy : IStrategy
{
    public string Name { get; } = "Simple coloring";
    
    public StrategyLevel Difficulty { get; } = StrategyLevel.Hard;
    public int Score { get; set; }

    public void ApplyOnce(IStrategyManager strategyManager)
    {
        for (int number = 1; number <= 9; number++)
        {
            List<ColorableWeb<ColoringCoordinate>> chains = new();
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    if (strategyManager.Possibilities[row, col].Peek(number))
                    {
                        ColoringCoordinate current = new(row, col);
                        if (DoesAnyChainContains(chains, current)) continue;
                        
                        ColorableWeb<ColoringCoordinate> web = new();
                        InitChain(strategyManager, web, current, number);
                        if (web.Count >= 2)
                        {
                            web.StartColoring();
                            chains.Add(web);
                        }
                    }
                }
            }

            foreach (var chain in chains)
            {
                SearchForTwiceInTheSameUnit(strategyManager, number, chain);
                SearchForTwoColorsElsewhere(strategyManager, number, chain);
            }
        }
    }

    private void SearchForTwiceInTheSameUnit(IStrategyManager strategyManager, int number, ColorableWeb<ColoringCoordinate> web)
    {
        web.ForEachCombinationOfTwo((one, two) =>
        {
            if (one.ShareAUnit(two) && one.Coloring == two.Coloring)
            {
                foreach (var coord in web)
                {
                    if (coord.Coloring == one.Coloring)
                        strategyManager.RemovePossibility(number, coord.Row, coord.Col, this);
                    else
                        strategyManager.AddDefinitiveNumber(number, coord.Row, coord.Col, this);
                }
            }

            return false;
        });
    }

    private void SearchForTwoColorsElsewhere(IStrategyManager strategyManager, int number, ColorableWeb<ColoringCoordinate> web)
    {
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                if (strategyManager.Possibilities[row, col].Peek(number))
                {
                    ColoringCoordinate current = new(row, col);
                    if (web.Contains(current)) continue;

                    bool[] onAndOff = new bool[2];
                    foreach (var coord in web)
                    {
                        if (coord.ShareAUnit(current))
                        {
                            onAndOff[(int)(coord.Coloring - 1)] = true;
                            if (onAndOff[0] && onAndOff[1])
                            {
                                strategyManager.RemovePossibility(number, row, col, this);
                                break;
                            }
                        }
                    }
                }
            }
        }
    }

    private void InitChain(IStrategyManager strategyManager, ColorableWeb<ColoringCoordinate> web, ColoringCoordinate current, int number)
    {
        var ppir = strategyManager.PossibilityPositionsInRow(current.Row, number);
        if (ppir.Count == 2)
        {
            foreach (var col in ppir)
            {
                if (col != current.Col)
                {
                    ColoringCoordinate next = new ColoringCoordinate(current.Row, col);
                    if(web.AddLink(current, next)) InitChain(strategyManager, web, next, number);
                    break;
                }
            }
        }
        
        var ppic = strategyManager.PossibilityPositionsInColumn(current.Col, number);
        if (ppic.Count == 2)
        {
            foreach (var row in ppic)
            {
                if (row != current.Row)
                {
                    ColoringCoordinate next = new ColoringCoordinate(row, current.Col);
                    if(web.AddLink(current, next)) InitChain(strategyManager, web, next, number);
                    break;
                }
            }
        }
        
        var ppimn = strategyManager.PossibilityPositionsInMiniGrid(current.Row / 3, current.Col / 3, number);
        if (ppimn.Count == 2)
        {
            foreach (var pos in ppimn)
            {
                if (pos[0] != current.Row && pos[1] != current.Col)
                {
                    ColoringCoordinate next = new ColoringCoordinate(pos[0], pos[1]);
                    if(web.AddLink(current, next)) InitChain(strategyManager, web, next, number);
                    break;
                }
            }
        }
    }

    private static bool DoesAnyChainContains(IEnumerable<ColorableWeb<ColoringCoordinate>> chains, ColoringCoordinate coord)
    {
        foreach (var chain in chains)
        {
            if (chain.Contains(coord)) return true;
        }

        return false;
    }
}