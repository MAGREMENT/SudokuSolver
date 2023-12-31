﻿using System.Collections.Generic;
using System.Text;
using Global.Enums;
using Model.Solver.Helpers.Highlighting;
using Model.Solver.StrategiesUtility.CellColoring;
using Model.Solver.StrategiesUtility.Graphs;

namespace Model.Solver.StrategiesUtility;

public static class ForcingNetsUtility
{
    public static void HighlightAllPaths(IHighlightable lighter, List<LinkGraphChain<ILinkGraphElement>> paths, Coloring startColoring)
    {
        HashSet<ILinkGraphElement> alreadyHighlighted = new();

        foreach (var path in paths)
        {
            for (int i = path.Links.Length - 1; i >= 0; i--)
            {
                var from = path.Elements[i];
                var to = path.Elements[i + 1];
                var link = path.Links[i];

                if (alreadyHighlighted.Contains(to)) break;
                
                lighter.HighlightLinkGraphElement(to, link == LinkStrength.Strong ? ChangeColoration.CauseOnOne : ChangeColoration.CauseOffOne);
                lighter.CreateLink(from, to, link);
                alreadyHighlighted.Add(to);
            }
            
            var first = path.Elements[0];

            if (!alreadyHighlighted.Contains(first))
            {
                lighter.HighlightLinkGraphElement(first, startColoring == Coloring.On ?
                    ChangeColoration.CauseOnOne : ChangeColoration.CauseOffOne);
                alreadyHighlighted.Add(first);
            }
        }
    }

    public static string AllPathsToString(List<LinkGraphChain<ILinkGraphElement>> paths)
    {
        var builder = new StringBuilder();

        for (int i = 0; i < paths.Count; i++)
        {
            var letter = (char)('a' + i);
            builder.Append($"{letter}) {paths[i]}\n");
        }

        return builder.ToString();
    }

    public static List<LinkGraphChain<ILinkGraphElement>> FindEveryNeededPaths(LinkGraphChain<ILinkGraphElement> basePath,
        IColoringResult<ILinkGraphElement> result, LinkGraph<ILinkGraphElement> graph, IPossibilitiesHolder snapshot)
    {
        var list = new List<LinkGraphChain<ILinkGraphElement>> {basePath};
        HashSet<ILinkGraphElement> allElements = new(basePath.Elements);
        Queue<LinkGraphChain<ILinkGraphElement>> queue = new();
        queue.Enqueue(basePath);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            for (int i = 0; i < current.Links.Length; i++)
            {
                if (current.Elements[i] is not CellPossibility from) continue;
                if (current.Elements[i + 1] is not CellPossibility to) continue;

                var currentLink = current.Links[i];
                if (currentLink != LinkStrength.Strong ||
                    graph.HasLinkTo(current.Elements[i], current.Elements[i + 1], LinkStrength.Strong)) continue;

                foreach (var offCell in FindOffCellsInJumpLinks(result, snapshot, from, to))
                {
                    if (allElements.Contains(offCell)) continue;

                    var path = result.History!.GetPathToRootWithGuessedLinks(offCell, Coloring.Off);
                    list.Add(path);
                    allElements.UnionWith(path.Elements);
                    queue.Enqueue(path);
                }
            }
        }

        return list;
    }

    private static List<CellPossibility> FindOffCellsInJumpLinks(IColoringResult<ILinkGraphElement> result,
        IPossibilitiesHolder snapshot, CellPossibility from, CellPossibility to)
    {
        List<CellPossibility>? best = null;
        
        if (from.Possibility == to.Possibility)
        {
            if (from.Row == to.Row)
            {
                var cols = snapshot.RowPositionsAt(from.Row, from.Possibility);
                bool ok = true;

                foreach (var col in cols)
                {
                    if (col == from.Column || col == to.Column) continue;
                    var current = new CellPossibility(from.Row, col, from.Possibility);

                    if (result.TryGetColoredElement(current, out var coloring) && coloring == Coloring.Off)
                        continue;

                    ok = false;
                    break;
                }

                if (ok)
                {
                    var buffer = new List<CellPossibility>();
                    foreach (var col in cols)
                    {
                        if (col == from.Column || col == to.Column) continue;

                        buffer.Add(new CellPossibility(from.Row, col, from.Possibility));
                    }

                    if (best is null || buffer.Count < best.Count) best = buffer;
                }
            }

            if (from.Column == to.Column)
            {
                var rows = snapshot.ColumnPositionsAt(from.Column, from.Possibility);
                bool ok = true;

                foreach (var row in rows)
                {
                    if (row == from.Row || row == to.Row) continue;
                    var current = new CellPossibility(row, from.Column, from.Possibility);

                    if (result.TryGetColoredElement(current, out var coloring) && coloring == Coloring.Off)
                        continue;

                    ok = false;
                    break;
                }

                if (ok)
                {
                    var buffer = new List<CellPossibility>();
                    foreach (var row in rows)
                    {
                        if (row == from.Row || row == to.Row) continue;

                        buffer.Add(new CellPossibility(row, from.Column, from.Possibility));
                    }
                    
                    if (best is null || buffer.Count < best.Count) best = buffer;
                }
            }

            if (from.Row / 3 == to.Row / 3 && from.Column / 3 == to.Column / 3)
            {
                var positions = snapshot.MiniGridPositionsAt(from.Row / 3, from.Column / 3, from.Possibility);
                bool ok = true;

                foreach (var pos in positions)
                {
                    var current = new CellPossibility(pos, from.Possibility);
                    if (current == from || current == to) continue;

                    if (result.TryGetColoredElement(current, out var coloring) && coloring == Coloring.Off)
                        continue;

                    ok = false;
                    break;
                }

                if (ok)
                {
                    var buffer = new List<CellPossibility>();
                    foreach (var pos in positions)
                    {
                        var current = new CellPossibility(pos, from.Possibility);
                        if (current == from || current == to) continue;

                        buffer.Add(new CellPossibility(pos, from.Possibility));
                    }
                    
                    if (best is null || buffer.Count < best.Count) best = buffer;
                }
            }
        }
        else if (from.Row == to.Row && from.Column == to.Column)
        {
            var possibilities = snapshot.PossibilitiesAt(from.Row, from.Column);
            bool ok = true;

            foreach (var pos in possibilities)
            {
                if (pos == from.Possibility || pos == to.Possibility) continue;

                var current = new CellPossibility(from.Row, from.Column, pos);
                if (result.TryGetColoredElement(current, out var coloring) && coloring == Coloring.Off)
                    continue;

                ok = false;
                break;
            }

            if (ok)
            {
                var buffer = new List<CellPossibility>();
                foreach (var pos in possibilities)
                {
                    if (pos == from.Possibility || pos == to.Possibility) continue;

                    buffer.Add(new CellPossibility(from.Row, from.Column, pos));
                }
                
                if (best is null || buffer.Count < best.Count) best = buffer;
            }
        }

        return best ?? new List<CellPossibility>();
    }

}