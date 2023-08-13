﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Model;
using Model.Logs;
using Model.Possibilities;
using Model.StrategiesUtil;

namespace SudokuSolver;

public partial class SolverUserControl : IHighlighter
{
    private const int CellSize = 57;
    private const int LineWidth = 3;
    
    private readonly Solver _solver = new(new Sudoku());
    private int _logBuffer = -1;

    private SudokuTranslationType _translationType = SudokuTranslationType.Shortcuts;

    private readonly SolverBackgroundManager _backgroundManager;

    public delegate void OnReady();
    public event OnReady? IsReady;

    public delegate void OnCellClicked(CellUserControl sender, int row, int col);
    public event OnCellClicked? CellClickedOn;

    public delegate void OnSolverUpdate(string solverAsString);
    public event OnSolverUpdate? SolverUpdated;
    
    public SolverUserControl()
    {
        InitializeComponent();

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
    }
    
    private void Update()
    {
        RefreshSolver();
        SolverUpdated?.Invoke(_solver.Sudoku.AsString(_translationType));
    }

    private void RefreshSolver()
    {
        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                UpdateCell(GetTo(i, j), i, j);
            }
        }
        
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
        _solver.SetDefinitiveNumberByHand(number, row, col); 
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
        
        _backgroundManager.Clear();

        if (_solver.Logs.Count > 0)
        {
            var current = _solver.Logs[^1];
            if (current.Id != _logBuffer)
            {
                Highlight(current);
                _logBuffer = current.Id;
            }
        }

        await Task.Delay(TimeSpan.FromMilliseconds(500));
        
        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                UpdateCell(GetTo(i, j), i, j);
            }
        }

        SolverUpdated?.Invoke(_solver.Sudoku.AsString(_translationType));
        IsReady?.Invoke();
    }

    public void ShowLog(ISolverLog log)
    {
        _backgroundManager.Clear();
        
        int n = -1;
        int cursor = 0;
        bool possibility = false;
        IPossibilities buffer = IPossibilities.NewEmpty();
        while (cursor < log.SolverState.Length)
        {
            char current = log.SolverState[cursor];
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

        Highlight(log);
    }
    
    public void ShowCurrent()
    {
        RefreshSolver();
    }

    public void HighlightPossibility(int possibility, int row, int col, ChangeColoration coloration)
    {
        _backgroundManager.HighlightPossibility(row, col, possibility, ColorUtil.ToColor(coloration));
    }

    public void HighlightCell(int row, int col, ChangeColoration coloration)
    {
        _backgroundManager.HighlightCell(row, col, ColorUtil.ToColor(coloration));
    }

    public void HighlightLinkGraphElement(ILinkGraphElement element, ChangeColoration coloration)
    {
        switch (element)
        {
            case PossibilityCoordinate coord :
                _backgroundManager.HighlightPossibility(coord.Row, coord.Col, coord.Possibility, ColorUtil.ToColor(coloration));
                break;
            case PointingRow pr :
                _backgroundManager.HighlightGroup(pr, ColorUtil.ToColor(coloration));
                break;
            case PointingColumn pc :
                _backgroundManager.HighlightGroup(pc, ColorUtil.ToColor(coloration));
                break;
        }
    }

    public void CreateLink(PossibilityCoordinate from, PossibilityCoordinate to, LinkStrength linkStrength)
    {
        _backgroundManager.CreateLink(from, to, linkStrength == LinkStrength.Strong ? DashStyles.Solid : DashStyles.Dash);
    }

    public void CreateLink(ILinkGraphElement from, ILinkGraphElement to, LinkStrength linkStrength)
    {
        if(from is PossibilityCoordinate one && to is PossibilityCoordinate two)
            _backgroundManager.CreateLink(one, two, linkStrength == LinkStrength.Strong ? DashStyles.Solid : DashStyles.Dash);
        else
        {
            PossibilityCoordinate[] winners = new PossibilityCoordinate[2];
            double winningDistance = int.MaxValue;

            foreach (var c1 in from.EachElement())
            {
                foreach (var c2 in to.EachElement())
                {
                    var distance = Math.Sqrt(Math.Pow(c1.Row - c2.Row, 2) + Math.Pow(c1.Col - c2.Col, 2));

                    if (distance < winningDistance)
                    {
                        winningDistance = distance;
                        winners[0] = c1;
                        winners[1] = c2;
                    }
                }
            }
            
            _backgroundManager.CreateLink(winners[0], winners[1], linkStrength == LinkStrength.Strong ? DashStyles.Solid : DashStyles.Dash);
        }
    }

    private void Highlight(ISolverLog log)
    {
        log.SolverHighLighter(this);
        Main.Background = _backgroundManager.Background;
    }

    public List<ISolverLog> GetLogs()
    {
        return _solver.Logs;
    }

    public IStrategy[] GetStrategies()
    {
        return _solver.Strategies;
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
    public int Margin { get; }

    public Brush Background
    {
        get
        {
            DrawingGroup current = new DrawingGroup();
            current.Children.Add(_grid);
            current.Children.Add(_cells);
            current.Children.Add(_cursor);
            current.Children.Add(_groups);
            current.Children.Add(_links);

            return new DrawingBrush(current);
        }
    }

    private readonly DrawingGroup _grid = new();
    private readonly DrawingGroup _cells = new();
    private readonly DrawingGroup _cursor = new();
    private readonly DrawingGroup _groups = new();
    private readonly DrawingGroup _links = new();

    private Coordinate? _currentCursor;

    public SolverBackgroundManager(int cellSize, int margin)
    {
        CellSize = cellSize;
        Margin = margin;
        Size = cellSize * 9 + margin * 10;

        List<GeometryDrawing> after = new();
        int start = 0;
        for (int i = 0; i < 10; i++)
        {
            if (i is 3 or 6)
            {
                after.Add(new GeometryDrawing()
                {
                    Geometry = new RectangleGeometry(new Rect(start, 0, margin, Size)),
                    Brush = Brushes.Black
                });
            
                after.Add(new GeometryDrawing()
                {
                    Geometry = new RectangleGeometry(new Rect(0, start, Size, margin)),
                    Brush = Brushes.Black
                }); 
            }
            else
            {
                _grid.Children.Add(new GeometryDrawing()
                {
                    Geometry = new RectangleGeometry(new Rect(start, 0, margin, Size)),
                    Brush = Brushes.Gray
                });
            
                _grid.Children.Add(new GeometryDrawing()
                {
                    Geometry = new RectangleGeometry(new Rect(0, start, Size, margin)),
                    Brush = Brushes.Gray
                }); 
            }

            foreach (var a in after)
            {
                _grid.Children.Add(a);
            }

            start += margin + cellSize;
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
        int startCol = row * CellSize + (row + 1) * Margin;
        int startRow = col * CellSize + (col + 1) * Margin;
        
        _cells.Children.Add(new GeometryDrawing()
        {
            Geometry = new RectangleGeometry(new Rect(startRow, startCol, CellSize, CellSize)),
            Brush = new SolidColorBrush(color)
        });
    }

    public void HighlightPossibility(int row, int col, int possibility, Color color)
    {
        int oneThird = CellSize / 3;
        
        int startCol = row * CellSize + (row + 1) * Margin + (possibility - 1) / 3 * oneThird;
        int startRow = col * CellSize + (col + 1) * Margin + (possibility - 1) % 3 * oneThird;
        
        _cells.Children.Add(new GeometryDrawing()
        {
            Geometry = new RectangleGeometry(new Rect(startRow, startCol, oneThird, oneThird)),
            Brush = new SolidColorBrush(color)
        });
    }
    
    public void HighlightGroup(PointingRow pr, Color color)
    {
        var coords = pr.EachElement();
        var mostLeft = coords[0];
        var mostRight = coords[0];
        for (int i = 1; i < coords.Length; i++)
        {
            if (coords[i].Col < mostLeft.Col) mostLeft = coords[i];
            if (coords[i].Col > mostRight.Col) mostRight = coords[i];
        }

        int oneThird = CellSize / 3;
        _groups.Children.Add(new GeometryDrawing()
        {
            Geometry = new RectangleGeometry(new Rect(TopLeftX(mostLeft.Col, pr.Possibility),
                TopLeftY(mostLeft.Row, pr.Possibility),
                (CellSize + Margin) * (mostRight.Col - mostLeft.Col) + oneThird, oneThird)),
            Pen = new Pen()
            {
            Thickness = 3.0,
            Brush = _linkBrush,
            DashStyle = DashStyles.DashDot
            }          
        });

        foreach (var coord in coords)
        {
            HighlightPossibility(coord.Row, coord.Col, pr.Possibility, color);
        }
    }

    public void HighlightGroup(PointingColumn pc, Color color)
    {
        var coords = pc.EachElement();
        var mostUp = coords[0];
        var mostDown = coords[0];
        for (int i = 1; i < coords.Length; i++)
        {
            if (coords[i].Row < mostUp.Row) mostUp = coords[i];
            if (coords[i].Row > mostDown.Row) mostDown = coords[i];
        }

        int oneThird = CellSize / 3;
        _groups.Children.Add(new GeometryDrawing()
        {
            Geometry = new RectangleGeometry(new Rect(TopLeftX(mostUp.Col, pc.Possibility),
                TopLeftY(mostUp.Row, pc.Possibility), oneThird,
                (CellSize + Margin) * (mostDown.Row - mostUp.Row) + oneThird)),
            Pen = new Pen()
            {
                Thickness = 3.0,
                Brush = _linkBrush,
                DashStyle = DashStyles.DashDot
            }          
        });
        
        foreach (var coord in coords)
        {
            HighlightPossibility(coord.Row, coord.Col, pc.Possibility, color);
        }
    }

    public void CreateLink(PossibilityCoordinate one, PossibilityCoordinate two, DashStyle dashStyle)
    {
        double oneThird = (double)CellSize / 3; 

        var from = new Point(one.Col * CellSize + (one.Col + 1) * Margin + (one.Possibility - 1) % 3 * oneThird + oneThird / 2,
            one.Row * CellSize + (one.Row + 1) * Margin + (one.Possibility - 1) / 3 * oneThird + oneThird / 2);
        var to = new Point(two.Col * CellSize + (two.Col + 1) * Margin + (two.Possibility - 1) % 3 * oneThird + oneThird / 2,
            two.Row * CellSize + (two.Row + 1) * Margin + (two.Possibility - 1) / 3 * oneThird + oneThird / 2);
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
        var space = CellSize / 5.5;
        var proportion = space / Math.Sqrt(Math.Pow(to.X - from.X, 2) + Math.Pow(to.Y - from.Y, 2));
        var newFrom = new Point(from.X + proportion * (to.X - from.X), from.Y + proportion * (to.Y - from.Y));
        
        AddLine(newFrom, to, dashStyle);
    }
    
    private void AddShortenedLine(Point from, Point middle, Point to, DashStyle dashStyle)
    {
        var space = CellSize / 5.5;
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
        
        if (_currentCursor is not null && _currentCursor == new Coordinate(row, col))
        {
            _currentCursor = null;
            return;
        }

        _currentCursor = new Coordinate(row, col);
        int startCol = row * CellSize + (row + 1) * Margin;
        int startRow = col * CellSize + (col + 1) * Margin;
        
        int oneFourth = CellSize / 4;
        
        //Top left corner
        _cursor.Children.Add(new GeometryDrawing()
        {
            Geometry = new RectangleGeometry(new Rect(startRow - Margin, startCol - Margin,
                oneFourth, Margin)),
            Brush = Brushes.Aqua
        });
        _cursor.Children.Add(new GeometryDrawing()
        {
            Geometry = new RectangleGeometry(new Rect(startRow - Margin, startCol - Margin,
                Margin, oneFourth)),
            Brush = Brushes.Aqua
        });
        
        //Top right corner
        _cursor.Children.Add(new GeometryDrawing()
        {
            Geometry = new RectangleGeometry(new Rect(startRow + CellSize + Margin - oneFourth, startCol - Margin,
                oneFourth, Margin)),
            Brush = Brushes.Aqua
        });
        _cursor.Children.Add(new GeometryDrawing()
        {
            Geometry = new RectangleGeometry(new Rect(startRow + CellSize, startCol - Margin,
                Margin, oneFourth)),
            Brush = Brushes.Aqua
        });
        
        //Bottom left corner
        _cursor.Children.Add(new GeometryDrawing()
        {
            Geometry = new RectangleGeometry(new Rect(startRow - Margin, startCol + CellSize,
                oneFourth, Margin)),
            Brush = Brushes.Aqua
        });
        _cursor.Children.Add(new GeometryDrawing()
        {
            Geometry = new RectangleGeometry(new Rect(startRow - Margin, startCol + CellSize + Margin - oneFourth,
                Margin, oneFourth)),
            Brush = Brushes.Aqua
        });
        
        //Bottom right corner
        _cursor.Children.Add(new GeometryDrawing()
        {
            Geometry = new RectangleGeometry(new Rect(startRow + CellSize + Margin - oneFourth, startCol + CellSize,
                oneFourth, Margin)),
            Brush = Brushes.Aqua
        });
        _cursor.Children.Add(new GeometryDrawing()
        {
            Geometry = new RectangleGeometry(new Rect(startRow + CellSize, startCol + CellSize + Margin - oneFourth,
                Margin, oneFourth)),
            Brush = Brushes.Aqua
        });
    }

    private int TopLeftX(int col)
    {
        return col * CellSize + (col + 1) * Margin;
    }

    private int TopLeftX(int col, int possibility)
    {
        int oneThird = CellSize / 3;
        return col * CellSize + (col + 1) * Margin + (possibility - 1) % 3 * oneThird;
    }

    private int TopLeftY(int row)
    {
        return row * CellSize + (row + 1) * Margin;  
    }

    private int TopLeftY(int row, int possibility)
    {
        int oneThird = CellSize / 3; 
        return row * CellSize + (row + 1) * Margin + (possibility - 1) / 3 * oneThird;
    }
}