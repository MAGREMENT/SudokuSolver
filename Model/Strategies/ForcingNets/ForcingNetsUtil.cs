﻿using System.Collections.Generic;
using Model.Changes;
using Model.StrategiesUtil;
using Model.StrategiesUtil.LinkGraph;

namespace Model.Strategies.ForcingNets;

public static class ForcingNetsUtil
{
    public static Dictionary<CellPossibility, Coloring> FilterPossibilityCoordinates(
        Dictionary<ILinkGraphElement, Coloring> coloring)
    {
        Dictionary<CellPossibility, Coloring> result = new();
        foreach (var element in coloring)
        {
            if (element.Key is not CellPossibility coord) continue;
            result.Add(coord, element.Value);
        }

        return result;
    }

    public static void HighlightColoring(IHighlightable lighter, Dictionary<CellPossibility, Coloring> coloring)
    {
        foreach (var element in coloring)
        {
            lighter.HighlightPossibility(element.Key, element.Value == Coloring.On ? ChangeColoration.CauseOnOne :
                ChangeColoration.CauseOffOne);
        }
    }
}