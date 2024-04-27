using System.Collections.Generic;

namespace Model.Sudokus.Solver.Strategies.MultiSector.Searchers;

public class VoidSearcher : ISetEquivalenceSearcher
{
    public IEnumerable<SetEquivalence> Search(ISudokuStrategyUser strategyUser)
    {
        yield break;
    }
}