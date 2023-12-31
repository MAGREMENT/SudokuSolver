﻿using System.Windows;
using System.Windows.Controls;
using Global;
using View.Themes;

namespace View.Canvas;

public partial class MultiChoiceOptionCanvas
{
    private readonly SetArgument<int> _setter;
    private readonly GetArgument<int> _getter;
    
    public MultiChoiceOptionCanvas(string name, string explanation, GetArgument<int> getter, 
        SetArgument<int> setter, params string[] choices)
    {
        InitializeComponent();

        _setter = setter;
        _getter = getter;

        TextBlock.Text = name;
        for (int i = 0; i < choices.Length; i++)
        {
            var radioButton = new RadioButton
            {
                Margin = new Thickness(0, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Content = choices[i]
            };

            var iForEvent = i;
            radioButton.Checked += (_, _) => TryChange(iForEvent);

            Panel.Children.Add(radioButton);
        }

        Explanation = explanation;
    }

    public override string Explanation { get; }
    public override void SetFontSize(int size)
    {
        TextBlock.FontSize = size;
    }

    public override void ApplyTheme(Theme theme)
    {
        TextBlock.Foreground = theme.Text;
        foreach (var radioButton in Panel.Children)
        {
            if (radioButton is not RadioButton rb) continue;
            rb.Foreground = theme.Text;
        }
    }

    protected override void InternalRefresh()
    {
        ((RadioButton)Panel.Children[_getter() + 1]).IsChecked = true;
    }

    private void TryChange(int i)
    {
        if (ShouldCallSetter) _setter(i);
    }
}