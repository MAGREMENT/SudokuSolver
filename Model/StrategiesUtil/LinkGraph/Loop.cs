﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Model.StrategiesUtil.LinkGraph;

// ReSharper disable All

namespace Model.StrategiesUtil.LoopFinder;

public class Loop<T> : IEnumerable<T> where T : ILoopElement
{
    private readonly T[] _elements;
    private readonly LinkStrength[] _links;

    public int Count => _elements.Length;

    public Loop(T[] elements, LinkStrength[] links)
    {
        _elements = elements;
        _links = links;
    }

    public Loop()
    {
        _elements = Array.Empty<T>();
        _links = Array.Empty<LinkStrength>();
    }

    public override int GetHashCode()
    {
        int hash = 0;
        foreach (T element in _elements)
        {
            hash ^= EqualityComparer<T>.Default.GetHashCode(element);
        }

        return hash;
    }

    public IEnumerator<T> GetEnumerator()
    {
        return _elements.AsEnumerable().GetEnumerator();
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Loop<T> loop) return false;
        if (loop._elements.Length != _elements.Length) return false;
        foreach (var element in _elements)
        {
            if (!loop._elements.Contains(element)) return false;
        }

        return true;
    }

    public override string ToString()
    {
        string result = _elements[0] + (_links[0] == LinkStrength.Strong ? "=" : "-");
        for (int i = 1; i < _elements.Length; i++)
        {
            result += _elements[i] + (_links[i] == LinkStrength.Strong ? "=" : "-");
        }

        return result;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public delegate void LinkHandler(T one, T two);

    public void ForEachLink(LinkHandler handler, LinkStrength strength)
    {
        for (int i = 0; i < _links.Length - 1; i++)
        {
            if (_links[i] == strength) handler(_elements[i], _elements[i + 1]);
        }

        if (_links[^1] == strength) handler(_elements[0], _elements[^1]);
    }
    
    public void ForEachLink(LinkHandler handler)
    {
        for (int i = 0; i < _links.Length - 1; i++)
        {
            handler(_elements[i], _elements[i + 1]);
        }

        handler(_elements[0], _elements[^1]);
    }

    public LinkStrength FindDoubleLink(out T value)
    {
        if (_links[0] == _links[^1])
        {
            value = _elements[0];
            return _links[0];
        }

        for (int i = 0; i < _links.Length; i++)
        {
            if (_links[i] == _links[i + 1])
            {
                value = _elements[i];
                return _links[i];
            }
        }

        value = default!;
        return LinkStrength.None;
    }

    public static Loop<T> operator ^(Loop<T> left, Loop<T> right)
    {
        List<int> leftInts = new();
        List<int> rightInts = new();
        for (int i = 0; i < left._elements.Length; i++)
        {
            for (int j = 0; j < right._elements.Length; j++)
            {
                if (left._elements[i].Equals(right._elements[j]))
                {
                    leftInts.Add(i);
                    rightInts.Add(j);
                }
            }
        }
        
        if(leftInts.Count < 2 || leftInts.Count == left.Count || leftInts.Count == right.Count) return new Loop<T>();

        int newTotal = left._elements.Length - leftInts.Count + right._elements.Length - leftInts.Count + 2; 
        T[] elements = new T[newTotal]; 
        LinkStrength[] links = new LinkStrength[newTotal];
        
        int elementLeftLength = leftInts[0] + 1;
        int linkLeftLength = leftInts[0];
        int elementCursor = elementLeftLength;
        int linkCurosr = linkLeftLength;

        Array.Copy(left._elements, 0, elements, 0, elementLeftLength);
        Array.Copy(left._links, 0, links, 0, linkLeftLength);

        bool positive = (rightInts[0] == right._elements.Length - 1 && rightInts[1] == 1) ||
                        rightInts[0] > rightInts[1];
        int n = rightInts[0];
        do
        {
            if (positive) n = (n + 1) % right._elements.Length;
            else
            {
                n = n - 1;
                n = n < 0 ? right._elements.Length + n : n;
            }
                
            elements[elementCursor++] = right._elements[n];
            links[linkCurosr++] = right._links[n];
        } while (rightInts[^1] != n);

        int start, length;
        if(leftInts[^1] < left._elements.Length - 1)
        {
            start = leftInts[^1] + 1;
            length = left._elements.Length - leftInts[^1] - 1;
            Array.Copy(left._elements, start, elements,
                elementCursor, length);
        }
        start = leftInts[^1];
        length = left._elements.Length - leftInts[^1];
        Array.Copy(left._links, start, links,
                linkCurosr, length);

        return new Loop<T>(elements, links);
    }
}

public class LoopBuilder<T> where T : ILoopElement
{

    private readonly T[] _elements;
    private readonly LinkStrength[] _links;

    public int Count => _elements.Length;

    public LoopBuilder(T first)
    {
        _elements = new[] { first };
        _links = Array.Empty<LinkStrength>();
    }

    private LoopBuilder(T[] elements, LinkStrength[] links)
    {
        _elements = elements;
        _links = links;
    }

    public LoopBuilder<T> Add(T to, LinkStrength strength)
    {
        T[] eBuffer = new T[_elements.Length + 1];
        LinkStrength[] lBuffer = new LinkStrength[_links.Length + 1];
        Array.Copy(_elements, eBuffer, _elements.Length);
        Array.Copy(_links, lBuffer, _links.Length);
        eBuffer[_elements.Length] = to;
        lBuffer[_links.Length] = strength;

        return new LoopBuilder<T>(eBuffer, lBuffer);
    }

    public LoopBuilder<T> Cut(int index)
    {
        T[] eBuffer = new T[_elements.Length - index];
        LinkStrength[] lBuffer = new LinkStrength[_links.Length - index];
        Array.Copy(_elements, index, eBuffer, 0, eBuffer.Length);
        Array.Copy(_links, index, lBuffer, 0, lBuffer.Length);
        
        return new LoopBuilder<T>(eBuffer, lBuffer);
    }

    public Loop<T> End(LinkStrength strength)
    {
        LinkStrength[] lBuffer = new LinkStrength[_links.Length + 1];
        Array.Copy(_links, lBuffer, _links.Length);
        lBuffer[_links.Length] = strength;

        return new Loop<T>(_elements, lBuffer);
    }

    public ContainedStatus Contains(T element)
    {
        if (_elements[0].Equals(element)) return ContainedStatus.First;
        for (int i = 1; i < _elements.Length; i++)
        {
            if (_elements[i].Equals(element)) return ContainedStatus.Contained;
        }

        return ContainedStatus.NotContained;
    }

    public bool IsAlreadyPresent(T element)
    {
        foreach (var e in _elements)
        {
            if (e.Equals(element)) return true;
        }

        return false;
    }

    public int IndexOf(T element)
    {
        for (int i = _elements.Length - 1; i >= 0; i--)
        {
            if (_elements[i].Equals(element)) return i;
        }

        return -1;
    }

    public T LastElement()
    {
        return _elements[^1];
    }

    public T? ElementBefore()
    {
        if (_elements.Length < 2) return default;
        return _elements[^2];
    }

    public T FirstElement()
    {
        return _elements[0];
    }

    public LinkStrength LastLink()
    {
        return _links.Length == 0 ? LinkStrength.None : _links[^1];
    }

    public LinkStrength FirstLink()
    {
        return _links.Length == 0 ? LinkStrength.None : _links[0];
    }

    public override string ToString()
    {
        string result = "";
        for (int i = 0; i < _elements.Length - 1; i++)
        {
            result += _elements[i] + (_links[i] == LinkStrength.Strong ? "=" : "-");
        }

        result += _elements[^1].ToString();

        return result;
    }

}

public enum ContainedStatus
{
    First, Contained, NotContained
}

public interface ILoopElement
{ 
    CellPossibilities[] EachElement();
}