﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Model.Utility;
using Model.Utility.Collections;

namespace Model.Helpers.Settings;

public interface ISettingInteractionInterface
{
    
}

public interface IStringListInteractionInterface : ISettingInteractionInterface, IEnumerable<string>
{
    public int[]? IndexTranslator { get; }
}

public class SliderInteractionInterface : ISettingInteractionInterface
{
    public SliderInteractionInterface(int min, int max, int tickFrequency)
    {
        Min = min;
        Max = max;
        TickFrequency = tickFrequency;
    }

    public int Min { get; }
    public int Max { get; }
    public int TickFrequency { get; }
}

public class MinMaxSliderInteractionInterface : ISettingInteractionInterface
{
    public MinMaxSliderInteractionInterface(int minMin, int minMax, int maxMin, int maxMax, int tickFrequency)
    {
        MinMin = minMin;
        MinMax = minMax;
        MaxMin = maxMin;
        MaxMax = maxMax;
        TickFrequency = tickFrequency;
    }

    public int MinMin { get; }
    public int MinMax { get; }
    public int MaxMin { get; }
    public int MaxMax { get; }
    public int TickFrequency { get; }
}

public class NameListInteractionInterface : IStringListInteractionInterface
{
    private readonly IReadOnlyList<INamed> _names;
    
    public NameListInteractionInterface(IReadOnlyList<INamed> names)
    {
        _names = names;
    }

    public IEnumerator<string> GetEnumerator()
    {
        foreach (var n in _names)
        {
            yield return n.Name;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int[]? IndexTranslator => null;
}

public class EnumListInteractionInterface<TEnum> : IStringListInteractionInterface where TEnum : struct, Enum
{
    private readonly IEnumerable<string> _enumerable;
    
    public int[]? IndexTranslator { get; }

    public EnumListInteractionInterface(IStringConverter? converter)
    {
        var buffer = Enum.GetNames<TEnum>();
        _enumerable = converter is null ? buffer : buffer.Select(converter.Convert);
        IndexTranslator = EnumConverter.ToIntArray<TEnum>();
    }
    
    public IEnumerator<string> GetEnumerator()
    {
        return _enumerable.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    
}

public class CheckBoxInteractionInterface : ISettingInteractionInterface
{
    
}