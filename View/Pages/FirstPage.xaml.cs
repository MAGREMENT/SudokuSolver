using System.Windows;

namespace View.Pages;

public partial class FirstPage
{
    private readonly IPageHandler _pageHandler;
    
    public FirstPage(IPageHandler pageHandler)
    {
        InitializeComponent();
        _pageHandler = pageHandler;
    }

    private void GoToSolver(object sender, RoutedEventArgs e)
    {
        _pageHandler.ShowPage(PagesName.Solver);
    }

    private void GoToStrategyManager(object sender, RoutedEventArgs e)
    {
        _pageHandler.ShowPage(PagesName.StrategyManager);
    }

    public override void OnShow()
    {
        
    }

    public override void OnQuit()
    {
        
    }
}