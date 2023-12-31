﻿using System.Windows;
using Global;
using View.Themes;

namespace View.Canvas;

public partial class CheckBoxOptionCanvas
{
    private readonly SetArgument<bool> _setter;
    private readonly GetArgument<bool> _getter;

    public CheckBoxOptionCanvas(string text, string explanation, GetArgument<bool> getter, SetArgument<bool> setter)
    {
        InitializeComponent();
        
        _setter = setter;
        _getter = getter;
        
        Box.Content = text;

        Explanation = explanation;
    }

    private void OnChecked(object sender, RoutedEventArgs e)
    {
        ChangeEvent();
    }

    private void OnUnchecked(object sender, RoutedEventArgs e)
    {
        ChangeEvent();
    }

    private void ChangeEvent()
    {
        if (!ShouldCallSetter) return;
        
        var val = Box.IsChecked;
        if (val is null) return;

        _setter(val.Value);
    }

    public override string Explanation { get; }
    public override void SetFontSize(int size)
    {
        Box.FontSize = size;
    }

    public override void ApplyTheme(Theme theme)
    {
        Box.Foreground = theme.Text;
    }

    protected override void InternalRefresh()
    {
        Box.IsChecked = _getter();
    }
}

