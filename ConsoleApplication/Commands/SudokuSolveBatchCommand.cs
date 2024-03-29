﻿using System.Text;
using Model.Sudoku;
using Model.Sudoku.Solver;
using Model.Sudoku.Solver.Trackers;

namespace ConsoleApplication.Commands;

public class SudokuSolveBatchCommand : Command
{
    private const int FileIndex = 0;
    private const int FeedbackIndex = 1;
    private const int WaitForAllIndex = 2;
    
    public override string Description => "Solves all the Sudoku's in a text file";
    
    public SudokuSolveBatchCommand() : base("SudokuSolveBatch",
        new Option("-f", "Text file containing the Sudoku's", OptionValueRequirement.Mandatory, OptionValueType.File),
        new Option("--feedback", "Feedback for each Sudoku"),
        new Option("-w", "Set all strategies behavior to wait for all"))
    {
    }
    
    public override void Execute(IReadOnlyArgumentInterpreter interpreter, IReadOnlyOptionsReport report)
    {
        if (!report.IsUsed(FileIndex))
        {
            Console.WriteLine("No file specified");
            return;
        }
        
        if (!interpreter.Instantiator.InstantiateSudokuSolver(out var solver)) return;

        var statistics = new StatisticsTracker();
        solver.AddTracker(statistics);

        if (report.IsUsed(FeedbackIndex)) statistics.NotifySolveDone = true;
        
        using TextReader reader = new StreamReader((string)report.GetValue(FileIndex)!, Encoding.UTF8);

        while (reader.ReadLine() is { } line)
        {
            int commentStart = line.IndexOf('#');
            var s = commentStart == -1 ? line : line[..commentStart];
            
            solver.SetSudoku(SudokuTranslator.TranslateLineFormat(s));
            if (report.IsUsed(WaitForAllIndex)) solver.Solve(InstanceHandling.UnorderedAll);
            else solver.Solve();
        }

        Console.WriteLine(statistics);
        solver.RemoveTracker(statistics);
    }
}