using System;
using System.Collections.Generic;
using Global.Enums;
using Model.Solver.StrategiesUtility.Graphs;

namespace Model.Solver.StrategiesUtility.Oddagons;

public class AlmostOddagon
{
    public LinkGraphLoop<CellPossibility> Loop { get; }
    public CellPossibility[] Guardians { get; }
    
    public AlmostOddagon(LinkGraphLoop<CellPossibility> loop, CellPossibility[] guardians)
    {
        Loop = loop;
        Guardians = guardians;
    }

    public static AlmostOddagon FromBoard(IPossibilitiesHolder holder, LinkGraphLoop<CellPossibility> loop)
    {
        List<CellPossibility> guardians = new();
        loop.ForEachLink((one, two) => guardians.AddRange(
                OddagonSearcher.FindGuardians(holder, one, two)), LinkStrength.Weak);

        return new AlmostOddagon(loop, guardians.ToArray());
    }
}