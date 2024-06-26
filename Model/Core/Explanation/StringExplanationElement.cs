﻿namespace Model.Core.Explanation;

public class StringExplanationElement : ExplanationElement
{
    private readonly string _value;
    
    public StringExplanationElement(string value)
    {
        _value = value;
    }

    public override string ToString()
    {
        return _value;
    }
    
    public override bool ShouldBeBold => false;
    public override ExplanationColor Color => ExplanationColor.TextDefault;

    public override void Show(IExplanationHighlighter highlighter)
    {
        
    }

    public override bool DoesShowSomething => false;
}