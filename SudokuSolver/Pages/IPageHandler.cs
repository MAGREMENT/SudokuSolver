namespace SudokuSolver.Pages;

public interface IPageHandler
{
    void ShowPage(PagesName pageName);
}

public enum PagesName
{
    First, Solver
}