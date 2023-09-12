﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Model;
using Model.Changes;
using Model.Logs;
using Model.Possibilities;
using Model.Solver;
using Model.StrategiesUtil;
using Model.StrategiesUtil.LinkGraph;
using SudokuSolver.Utils;

namespace SudokuSolver;

public partial class SolverUserControl : IHighlightable
{
    public const int CellSize = 60;
    private const int LineWidth = 3;
    
    private readonly Solver _solver = new(new Sudoku());
    private int _logBuffer;

    private SudokuTranslationType _translationType = SudokuTranslationType.Shortcuts;
    public int Delay { get; set; } = 400;

    private readonly SolverBackgroundManager _backgroundManager;

    public delegate void OnReady();
    public event OnReady? IsReady;

    public delegate void OnCellClicked(CellUserControl sender, int row, int col);
    public event OnCellClicked? CellClickedOn;

    public delegate void OnSolverUpdate(string solverAsString);
    public event OnSolverUpdate? SolverUpdated;

    public event LogManager.OnLogsUpdate? LogsUpdated;
    
    public SolverUserControl()
    {
        InitializeComponent();

        _solver.LogsUpdated += logs => LogsUpdated?.Invoke(logs);

        //Init background
        _backgroundManager = new SolverBackgroundManager(CellSize, LineWidth);
        Main.Width = _backgroundManager.Size;
        Main.Height = _backgroundManager.Size;
        
        //Init numbers
        for (int i = 0; i < 9; i++)
        {
            HorizontalNumbers.ColumnDefinitions.Add(new ColumnDefinition()
            {
                Width = new GridLength(LineWidth)
            });
            HorizontalNumbers.ColumnDefinitions.Add(new ColumnDefinition()
            {
                Width = new GridLength(CellSize)
            });
            var horizontal = new TextBlock()
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Text = (i + 1).ToString(),
                FontSize = 15,
                FontWeight = FontWeights.Bold
            };
            Grid.SetColumn(horizontal, 1 + i * 2);
            HorizontalNumbers.Children.Add(horizontal);
            
            VerticalNumbers.RowDefinitions.Add(new RowDefinition()
            {
                Height = new GridLength(LineWidth)
            });
            VerticalNumbers.RowDefinitions.Add(new RowDefinition()
            {
                Height = new GridLength(CellSize)
            });
            var vertical = new TextBlock()
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Text = (i + 1).ToString(),
                FontSize = 15,
                FontWeight = FontWeights.Bold
            };
            Grid.SetRow(vertical, 1 + i * 2);
            VerticalNumbers.Children.Add(vertical);
        }
        
        //Init cells
        for (int i = 0; i < 9; i++)
        {
            StackPanel row = (StackPanel)Main.Children[i];
            for (int j = 0; j < 9; j++)
            {
                var toAdd = new CellUserControl();
                toAdd.SetMargin(LineWidth, LineWidth, 0, 0);
                row.Children.Add(toAdd);

                int rowForEvent = i;
                int colForEvent = j;
                toAdd.ClickedOn += sender =>
                {
                    CellClickedOn?.Invoke(sender, rowForEvent, colForEvent);
                    _backgroundManager.PutCursorOn(rowForEvent, colForEvent);
                    Main.Background = _backgroundManager.Background;
                };
            }
        }

        RefreshSolver();
    }

    public void NewSudoku(Sudoku sudoku)
    {
        _solver.SetSudoku(sudoku);
        RefreshSolver();

        _logBuffer = 0;
    }
    
    private void Update()
    {
        RefreshSolver();
        SolverUpdated?.Invoke(_solver.Sudoku.AsString(_translationType));
    }

    private void RefreshSolver()
    {
        ShowCurrentState();
        
        _backgroundManager.Clear();
        Main.Background = _backgroundManager.Background;
    }
    
    private void UpdateCell(CellUserControl current, int row, int col)
    {
        if(_solver.Sudoku[row, col] != 0) current.SetDefinitiveNumber(_solver.Sudoku[row, col]);
        else current.SetPossibilities(_solver.Possibilities[row, col]);
    }


    public void AddDefinitiveNumber(int number, int row, int col)
    {
        _solver.SetSolutionByHand(number, row, col); 
        Update();
    }
    
    public void RemovePossibility(int number, int row, int col)
    {
        _solver.RemovePossibilityByHand(number, row, col);
        Update();
    }
    
    public void ClearSudoku()
    {
        _solver.SetSudoku(new Sudoku());
        Update();
    }

    public void SolveSudoku()
    {
        _solver.Solve();
        if (_solver.Logs.Count > 0) _logBuffer = _solver.Logs[^1].Id;
        Update();
        IsReady?.Invoke();
    }

    public async void RunUntilProgress()
    {
        _solver.Solve(true);

        if (_solver.Logs.Count > 0 && _solver.Logs[^1].Id == _logBuffer)
        {
            RefreshSolver();
            IsReady?.Invoke();
            return;
        }

        int start = _logBuffer;
        for (int n = _logBuffer; n < _solver.Logs.Count; n++)
        {
            if(n != start) await Task.Delay(TimeSpan.FromMilliseconds(Delay));
            
            _backgroundManager.Clear();
            
            var current = _solver.Logs[n];
            
            Highlight(current);
            await Task.Delay(TimeSpan.FromMilliseconds(Delay));
            
            ShowState(_solver.Logs[n].SolverState);

            _logBuffer = current.Id;
            SolverUpdated?.Invoke(_solver.Sudoku.AsString(_translationType));
        }
        
        IsReady?.Invoke();
    }

    public void ShowLog(ISolverLog log)
    {
        _backgroundManager.Clear();

        ShowState(log.SolverState);

        Highlight(log);
    }

    private void ShowCurrentState()
    {
        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                UpdateCell(GetTo(i, j), i, j);
            }
        }
    }

    public void ShowStartState()
    {
        ShowState(_solver.StartState);
        
        _backgroundManager.Clear();
        Main.Background = _backgroundManager.Background;
    }

    private void ShowState(string state)
    {
        int n = -1;
        int cursor = 0;
        bool possibility = false;
        IPossibilities buffer = IPossibilities.NewEmpty();
        while (cursor < state.Length)
        {
            char current = state[cursor];
            if (current is 'd' or 'p')
            {
                if (buffer.Count > 0)
                {
                    var scuc = GetTo(n / 9, n % 9);
                    if (possibility) scuc.SetPossibilities(buffer);
                    else scuc.SetDefinitiveNumber(buffer.GetFirst());

                    buffer.RemoveAll();
                }

                possibility = current == 'p';
                n++;
            }
            else buffer.Add(current - '0');

            cursor++;
        }
        
        var scuc2 = GetTo(n / 9, n % 9);
        if (possibility) scuc2.SetPossibilities(buffer);
        else scuc2.SetDefinitiveNumber(buffer.GetFirst());
    }
    
    public void ShowCurrent()
    {
        RefreshSolver();
    }

    public void HighlightPossibility(int possibility, int row, int col, ChangeColoration coloration)
    {
        _backgroundManager.HighlightPossibility(row, col, possibility, ColorUtil.ToColor(coloration));
    }

    public void CirclePossibility(int possibility, int row, int col)
    {
        _backgroundManager.CirclePossibility(row, col, possibility);
    }

    public void HighlightCell(int row, int col, ChangeColoration coloration)
    {
        _backgroundManager.HighlightCell(row, col, ColorUtil.ToColor(coloration));
    }

    public void CircleCell(int row, int col)
    {
        _backgroundManager.CircleCell(row, col);
    }

    public void HighlightLinkGraphElement(ILinkGraphElement element, ChangeColoration coloration)
    {
        switch (element)
        {
            case CellPossibility coord :
                _backgroundManager.HighlightPossibility(coord.Row, coord.Col, coord.Possibility, ColorUtil.ToColor(coloration));
                break;
            case PointingRow pr :
                _backgroundManager.HighlightGroup(pr, ColorUtil.ToColor(coloration));
                break;
            case PointingColumn pc :
                _backgroundManager.HighlightGroup(pc, ColorUtil.ToColor(coloration));
                break;
            case AlmostNakedPossibilities anp :
                _backgroundManager.HighlightGroup(anp, ColorUtil.ToColor(coloration));
                break;
        }
    }

    public void CreateLink(CellPossibility from, CellPossibility to, LinkStrength linkStrength)
    {
        _backgroundManager.CreateLink(from, to, linkStrength == LinkStrength.Strong ? DashStyles.Solid : DashStyles.Dot);
    }

    public void CreateLink(ILinkGraphElement from, ILinkGraphElement to, LinkStrength linkStrength)
    {
        switch (from)
        {
            case CellPossibility one when to is CellPossibility two:
                _backgroundManager.CreateLink(one, two, linkStrength == LinkStrength.Strong ? DashStyles.Solid : DashStyles.Dot);
                break;
            case CellPossibility when to is AlmostNakedPossibilities:
                break;
            default:
                CellPossibility[] winners = new CellPossibility[2];
                double winningDistance = int.MaxValue;

                foreach (var c1 in from.EachElement())
                {
                    foreach (var c2 in to.EachElement())
                    {
                        foreach (var pc1 in c1.ToPossibilityCoordinates())
                        {
                            foreach (var pc2 in c2.ToPossibilityCoordinates())
                            {
                                var distance = Math.Pow(pc1.Row - pc2.Row, 2) + Math.Pow(pc1.Col - pc2.Col, 2);

                                if (distance < winningDistance)
                                {
                                    winningDistance = distance;
                                    winners[0] = pc1;
                                    winners[1] = pc2;
                                }
                            }
                        }
                    }
                }
            
                _backgroundManager.CreateLink(winners[0], winners[1], linkStrength == LinkStrength.Strong ?
                    DashStyles.Solid : DashStyles.Dot);
                break;
            
        }
    }

    private void Highlight(ISolverLog log)
    {
        log.HighlightManager.Apply(this);
        Main.Background = _backgroundManager.Background;
    }

    public List<ISolverLog> GetLogs()
    {
        return _solver.Logs;
    }

    public StrategyInfo[] GetStrategies()
    {
        return _solver.StrategyInfos;
    }

    public void ExcludeStrategy(int number)
    {
        _solver.ExcludeStrategy(number);
    }

    public void UseStrategy(int number)
    {
        _solver.UseStrategy(number);
    }

    public void SetTranslationType(SudokuTranslationType type)
    {
        _translationType = type;
        SolverUpdated?.Invoke(_solver.Sudoku.AsString(type));
    }

    private CellUserControl GetTo(int row, int col)
    {
        return (CellUserControl) ((StackPanel)Main.Children[row]).Children[col];
    }

}

public class SolverBackgroundManager
{
    private readonly Brush _linkBrush = Brushes.Indigo;
    private const double LinkOffset = 20;

    public int Size { get; }
    public int CellSize { get; }
    private readonly double _oneThird;
    public int Margin { get; }

    public Brush Background
    {
        get
        {
            DrawingGroup current = new DrawingGroup();
            current.Children.Add(_cells);
            current.Children.Add(_grid);
            current.Children.Add(_cursor);
            current.Children.Add(_groups);
            current.Children.Add(_links);

            return new DrawingBrush(current);
        }
    }
    
    private readonly DrawingGroup _cells = new();
    private readonly DrawingGroup _grid = new();
    private readonly DrawingGroup _cursor = new();
    private readonly DrawingGroup _groups = new();
    private readonly DrawingGroup _links = new();

    private Cell? _currentCursor;

    public SolverBackgroundManager(int cellSize, int margin)
    {
        CellSize = cellSize;
        _oneThird = (double)cellSize / 3;
        Margin = margin;
        Size = cellSize * 9 + margin * 10;

        List<GeometryDrawing> after = new();
        int start = 0;
        for (int i = 0; i < 10; i++)
        {
            if (i is 3 or 6)
            {
                after.Add(GetRectangle(start, 0, margin, Size, Brushes.Black));
                after.Add(GetRectangle(0, start, Size, margin, Brushes.Black));
            }
            else
            {
                _grid.Children.Add(GetRectangle(start, 0, margin, Size, Brushes.Gray));
                _grid.Children.Add(GetRectangle(0, start, Size, margin, Brushes.Gray));
            }

            start += margin + cellSize;
        }
        
        foreach (var a in after)
        {
            _grid.Children.Add(a);
        }
    }

    public void Clear()
    {
        _cells.Children.Clear();
        _groups.Children.Clear();
        _links.Children.Clear();
    }

    public void HighlightCell(int row, int col, Color color)
    {
        _cells.Children.Add(GetSquare(TopLeftX(col), TopLeftY(row), CellSize, new SolidColorBrush(color)));
    }

    public void HighlightPossibility(int row, int col, int possibility, Color color)
    {
        _cells.Children.Add(GetSquare(TopLeftX(col, possibility), TopLeftY(row, possibility), _oneThird, new SolidColorBrush(color)));
    }

    public void CirclePossibility(int row, int col, int possibility)
    {
        _groups.Children.Add(new GeometryDrawing()
        {
            Geometry = new RectangleGeometry(new Rect(TopLeftX(col, possibility), TopLeftY(row, possibility), _oneThird, _oneThird)),
            Pen = new Pen()
            {
                Brush = _linkBrush,
                Thickness = 3.0,
                DashStyle = DashStyles.Dot
            }
        });
    }

    public void CircleCell(int row, int col)
    {
        _groups.Children.Add(new GeometryDrawing()
        {
            Geometry = new RectangleGeometry(new Rect(TopLeftX(col), TopLeftY(row), CellSize, CellSize)),
            Pen = new Pen()
            {
                Brush = _linkBrush,
                Thickness = 3.0,
                DashStyle = DashStyles.Dot
            }
        });
    }
    
    public void HighlightGroup(PointingRow pr, Color color)
    {
        var coords = pr.EachElement();
        var mostLeft = coords[0];
        var mostRight = coords[0];
        for (int i = 1; i < coords.Length; i++)
        {
            if (coords[i].Cell.Col < mostLeft.Cell.Col) mostLeft = coords[i];
            if (coords[i].Cell.Col > mostRight.Cell.Col) mostRight = coords[i];
        }

        _groups.Children.Add(new GeometryDrawing()
        {
            Geometry = new RectangleGeometry(new Rect(TopLeftX(mostLeft.Cell.Col, pr.Possibility),
                TopLeftY(mostLeft.Cell.Row, pr.Possibility),
                (CellSize + Margin) * (mostRight.Cell.Col - mostLeft.Cell.Col) + _oneThird, _oneThird)),
            Pen = new Pen()
            {
            Thickness = 3.0,
            Brush = new SolidColorBrush(color),
            DashStyle = DashStyles.DashDot
            }          
        });
    }

    public void HighlightGroup(PointingColumn pc, Color color)
    {
        var coords = pc.EachElement();
        var mostUp = coords[0];
        var mostDown = coords[0];
        for (int i = 1; i < coords.Length; i++)
        {
            if (coords[i].Cell.Row < mostUp.Cell.Row) mostUp = coords[i];
            if (coords[i].Cell.Row > mostDown.Cell.Row) mostDown = coords[i];
        }

        _groups.Children.Add(new GeometryDrawing()
        {
            Geometry = new RectangleGeometry(new Rect(TopLeftX(mostUp.Cell.Col, pc.Possibility),
                TopLeftY(mostUp.Cell.Row, pc.Possibility), _oneThird,
                (CellSize + Margin) * (mostDown.Cell.Row - mostUp.Cell.Row) + _oneThird)),
            Pen = new Pen()
            {
                Thickness = 3.0,
                Brush = new SolidColorBrush(color),
                DashStyle = DashStyles.DashDot
            }          
        });
    }

    public void HighlightGroup(AlmostNakedPossibilities anp, Color color)
    {
        foreach (var coord in anp.CoordinatePossibilities)
        {
            var x = TopLeftX(coord.Cell.Col);
            var y = TopLeftY(coord.Cell.Row);
            
            if(!anp.Contains(coord.Cell.Row - 1, coord.Cell.Col))
                _groups.Children.Add(new GeometryDrawing()
                {
                    Geometry = new LineGeometry(new Point(x, y), new Point(x + CellSize, y)),
                    Pen = new Pen()
                    {
                    Thickness = 3.0,
                    Brush = new SolidColorBrush(color),
                    DashStyle = DashStyles.DashDot 
                    }     
                });
            
            if(!anp.Contains(coord.Cell.Row + 1, coord.Cell.Col))
                _groups.Children.Add(new GeometryDrawing()
                {
                    Geometry = new LineGeometry(new Point(x, y + CellSize), new Point(x + CellSize, y + CellSize)),
                    Pen = new Pen()
                    {
                        Thickness = 3.0,
                        Brush = new SolidColorBrush(color),
                        DashStyle = DashStyles.DashDot 
                    }     
                });
            
            if(!anp.Contains(coord.Cell.Row, coord.Cell.Col - 1))
                _groups.Children.Add(new GeometryDrawing()
                {
                    Geometry = new LineGeometry(new Point(x, y), new Point(x, y + CellSize)),
                    Pen = new Pen()
                    {
                        Thickness = 3.0,
                        Brush = new SolidColorBrush(color),
                        DashStyle = DashStyles.DashDot 
                    }     
                });
            
            if(!anp.Contains(coord.Cell.Row, coord.Cell.Col + 1))
                _groups.Children.Add(new GeometryDrawing()
                {
                    Geometry = new LineGeometry(new Point(x + CellSize, y), new Point(x + CellSize, y + CellSize)),
                    Pen = new Pen()
                    {
                        Thickness = 3.0,
                        Brush = new SolidColorBrush(color),
                        DashStyle = DashStyles.DashDot 
                    }     
                });
        }
    }

    public void CreateLink(CellPossibility one, CellPossibility two, DashStyle dashStyle)
    {
        var from = new Point(one.Col * CellSize + (one.Col + 1) * Margin + (one.Possibility - 1) % 3 * _oneThird + _oneThird / 2,
            one.Row * CellSize + (one.Row + 1) * Margin + (one.Possibility - 1) / 3 * _oneThird + _oneThird / 2);
        var to = new Point(two.Col * CellSize + (two.Col + 1) * Margin + (two.Possibility - 1) % 3 * _oneThird + _oneThird / 2,
            two.Row * CellSize + (two.Row + 1) * Margin + (two.Possibility - 1) / 3 * _oneThird + _oneThird / 2);
        var middle = new Point(from.X + (to.X - from.X) / 2, from.Y + (to.Y - from.Y) / 2);

        double angle = Math.Atan((to.Y - from.Y) / (to.X - from.X));
        double reverseAngle = Math.PI - angle;

        var deltaX = LinkOffset * Math.Sin(reverseAngle);
        var deltaY = LinkOffset * Math.Cos(reverseAngle);
        var offsetOne = new Point(middle.X + deltaX, middle.Y + deltaY);
        if (offsetOne.X > 0 && offsetOne.X < Size && offsetOne.Y > 0 && offsetOne.Y < Size)
        {
            AddShortenedLine(from, offsetOne, to, dashStyle);
            return;
        }
        
        var offsetTwo = new Point(middle.X - deltaX, middle.Y - deltaY);
        if (offsetTwo.X > 0 && offsetTwo.X < Size && offsetTwo.Y > 0 && offsetTwo.Y < Size)
        {
            AddShortenedLine(from, offsetTwo, to, dashStyle);
            return;
        }

        AddShortenedLine(from, to, dashStyle);
    }

    private void AddShortenedLine(Point from, Point to, DashStyle dashStyle)
    {
        var space = (double)CellSize / 3;
        var proportion = space / Math.Sqrt(Math.Pow(to.X - from.X, 2) + Math.Pow(to.Y - from.Y, 2));
        var newFrom = new Point(from.X + proportion * (to.X - from.X), from.Y + proportion * (to.Y - from.Y));
        
        AddLine(newFrom, to, dashStyle);
    }
    
    private void AddShortenedLine(Point from, Point middle, Point to, DashStyle dashStyle)
    {
        var space = (double)CellSize / 3;
        var proportion = space / Math.Sqrt(Math.Pow(to.X - from.X, 2) + Math.Pow(to.Y - from.Y, 2));
        var newFrom = new Point(from.X + proportion * (middle.X - from.X), from.Y + proportion * (middle.Y - from.Y));
        var newTo = new Point(to.X + proportion * (middle.X - to.X), to.Y + proportion * (middle.Y - to.Y));
        
        AddLine(newFrom, middle, dashStyle);
        AddLine(middle, newTo, dashStyle);
    }

    private void AddLine(Point from, Point to, DashStyle dashStyle)
    {
        _links.Children.Add(new GeometryDrawing
        {
            Geometry = new LineGeometry(from, to),
            Pen = new Pen()
            {
                Thickness = 3.0,
                Brush = _linkBrush,
                DashStyle = dashStyle
            }
        });
    }

    public void PutCursorOn(int row, int col)
    {
        _cursor.Children.Clear();
        
        if (_currentCursor is not null && _currentCursor == new Cell(row, col))
        {
            _currentCursor = null;
            return;
        }

        _currentCursor = new Cell(row, col);
        int startCol = row * CellSize + (row + 1) * Margin;
        int startRow = col * CellSize + (col + 1) * Margin;
        
        int oneFourth = CellSize / 4;
        
        //Top left corner
        _cursor.Children.Add(GetRectangle(startRow - Margin, startCol - Margin, 
            oneFourth, Margin, Brushes.Aqua));
        _cursor.Children.Add(GetRectangle(startRow - Margin, startCol - Margin,
            Margin, oneFourth, Brushes.Aqua));

        //Top right corner
        _cursor.Children.Add(GetRectangle(startRow + CellSize + Margin - oneFourth, startCol - Margin,
            oneFourth, Margin, Brushes.Aqua));
        _cursor.Children.Add(GetRectangle(startRow + CellSize, startCol - Margin,
            Margin, oneFourth, Brushes.Aqua));

        //Bottom left corner
        _cursor.Children.Add(GetRectangle(startRow - Margin, startCol + CellSize,
            oneFourth, Margin, Brushes.Aqua));
        _cursor.Children.Add(GetRectangle(startRow - Margin, startCol + CellSize + Margin - oneFourth,
            Margin, oneFourth, Brushes.Aqua));

        //Bottom right corner
        _cursor.Children.Add(GetRectangle(startRow + CellSize + Margin - oneFourth, startCol + CellSize,
            oneFourth, Margin, Brushes.Aqua));
        _cursor.Children.Add(GetRectangle(startRow + CellSize, startCol + CellSize + Margin - oneFourth,
            Margin, oneFourth, Brushes.Aqua));
    }
    
    private const double PenStrokeWidth = 0.5;

    private GeometryDrawing GetRectangle(double topLeftX, double topLeftY, double width, double height, Brush brush)
    {
        return new GeometryDrawing
        {
            Geometry = new RectangleGeometry(new Rect(topLeftX, topLeftY, width, height)),
            Brush = brush,
            Pen = new Pen{
                Brush = brush,
                Thickness = PenStrokeWidth
            }
        };
    }

    private GeometryDrawing GetSquare(double topLeftX, double topLeftY, double size, Brush brush)
    {
        return GetRectangle(topLeftX, topLeftY, size, size, brush);
    }

    private double TopLeftX(int col)
    {
        return col * CellSize + (col + 1) * Margin;
    }

    private double TopLeftX(int col, int possibility)
    {
        return col * CellSize + (col + 1) * Margin + (possibility - 1) % 3 * _oneThird;
    }

    private double TopLeftY(int row)
    {
        return row * CellSize + (row + 1) * Margin;  
    }

    private double TopLeftY(int row, int possibility)
    {
        return row * CellSize + (row + 1) * Margin + (possibility - 1) / 3 * _oneThird;
    }
}