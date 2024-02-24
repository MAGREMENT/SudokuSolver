﻿using Model.Sudoku;
using Model.Sudoku.Generator;
using Model.Sudoku.Solver;
using Repository;

namespace ConsoleApplication.Commands;

public class GenerateBatchCommand : Command
{
    private const int CountIndex = 0;
    private const int RateIndex = 1;
    private const int HardestIndex = 2;
    private const int SortIndex = 3;
    
    public override string Description => "Generate a determined amount of sudoku's";
    
    private readonly ISudokuPuzzleGenerator _generator = new RCRSudokuPuzzleGenerator(new BackTrackingFilledSudokuGenerator());
    
    public GenerateBatchCommand() : base("GenerateBatch", 
        new Option("-c", "Count", OptionValueRequirement.Mandatory, OptionValueType.Int),
        new Option("-r", "Rate puzzles difficulty"),
        new Option("-h", "Find hardest strategy used"),
        new Option("-s", "Sort sudoku's"))
    {
    }
    
    public override void Execute(IReadOnlyArgumentInterpreter interpreter, IReadOnlyOptionsReport report)
    {
        var count = report.IsUsed(CountIndex) ? (int)report.GetValue(CountIndex)! : 1;
        
        Console.WriteLine("Started generating...");
        var start = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        var generated = _generator.Generate(count);
        Console.WriteLine($"Finished generating in {Math.Round((double)(DateTimeOffset.Now.ToUnixTimeMilliseconds() - start) / 1000, 4)}s");

        List<GeneratedSudoku> result = new(count);

        if (report.IsUsed(RateIndex) || report.IsUsed(HardestIndex))
        {
            var repo = new SudokuStrategiesJSONRepository("strategies.json");
            if (!repo.Initialize(false))
            {
                Console.WriteLine("Exception while initializing repository");
                return;
            }

            var solver = new SudokuSolver();
            solver.StrategyManager.AddStrategies(repo.Download());

            var ratings = report.IsUsed(RateIndex) ? new RatingTracker(solver) : null;
            var hardest = report.IsUsed(HardestIndex) ? new HardestStrategyTracker(solver) : null;
            
            Console.WriteLine("Started evaluating...");
            start = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            foreach (var s in generated)
            {
                solver.SetSudoku(s.Copy());
                solver.Solve();
                result.Add(new GeneratedSudoku(SudokuTranslator.TranslateLineFormat(s, SudokuTranslationType.Points),
                    ratings?.Rating ?? 0, hardest?.Hardest));
            
                ratings?.Clear();
                hardest?.Clear();
            }
            Console.WriteLine($"Finished evaluating in {Math.Round((double)(DateTimeOffset.Now.ToUnixTimeMilliseconds() - start) / 1000, 4)}s");
        }
        else
        {
            foreach (var s in generated)
            {
                result.Add(new GeneratedSudoku(SudokuTranslator.TranslateLineFormat(s, SudokuTranslationType.Points),
                    0, null));
            }
        }

        if (report.IsUsed(SortIndex))
        {
            Console.WriteLine("Started sorting...");
            start = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            result.Sort((s1, s2) =>
            {
                var r = (int)(s2.Rating * 1000 - s1.Rating * 1000);
                if (r != 0) return r;

                if (s1.HardestStrategy is null || s2.HardestStrategy is null) return 0;

                return (int)s2.HardestStrategy.Difficulty - (int)s1.HardestStrategy.Difficulty;
            });
            Console.WriteLine($"Finished sorting in {Math.Round((double)(DateTimeOffset.Now.ToUnixTimeMilliseconds() - start) / 1000, 4)}s");
        }
        
        var n = 1;
        foreach (var s in result)
        {
            Console.Write($"#{n++} {s.Sudoku}");
            if(s.Rating != 0) Console.Write($" - {Math.Round(s.Rating, 2)}");
            if(s.HardestStrategy is not null) Console.Write($" - {s.HardestStrategy}");
            Console.WriteLine();
        }
    }
}

public record GeneratedSudoku(string Sudoku, double Rating, SudokuStrategy? HardestStrategy);