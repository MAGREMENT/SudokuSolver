﻿using System.Collections;
using System.Collections.Generic;

namespace Model.Possibilities;

public class BitPossibilities : IPossibilities
{
    private byte _possibilities;
    public int Count { private set; get; }

    private BitPossibilities(byte possibilities, int count)
    {
        _possibilities = possibilities;
        Count = count;
    }

    public BitPossibilities()
    {
        _possibilities = 0xFF;
        Count = 9;
    }
    
    public bool Remove(int number)
    {
        int index = number - 1;
        bool old = ((_possibilities >> index) & 1) > 0;
        _possibilities |= (byte) (1 << index);
        if (old) Count--;
        return old;
    }

    public void RemoveAll()
    {
        _possibilities = 0;
        Count = 0;
    }

    public void RemoveAll(params int[] except)
    {
        RemoveAll();
        foreach (var num in except)
        {
            _possibilities |= (byte) (1 << (num - 1));
            Count++;
        }
    }

    public void RemoveAll(IEnumerable<int> except)
    {
        RemoveAll();
        foreach (var num in except)
        {
            _possibilities |= (byte) (1 << (num - 1));
            Count++;
        }
    }

    public IPossibilities Mash(IPossibilities possibilities)
    {
        if (possibilities is BitPossibilities bp)
        {
            byte mashed = (byte) (this._possibilities | bp._possibilities);
            return new BitPossibilities(mashed, System.Numerics.BitOperations.PopCount(mashed));
        }
        return IPossibilities.DefaultMash(this, possibilities);
    }

    public bool Peek(int number)
    {
        return ((_possibilities >> (number - 1)) & 1) > 0;
    }

    public IEnumerable<int> All()
    {
        for (int i = 0; i < 9; i++)
        {
            if (((_possibilities >> i) & 1) > 0)
            {
                yield return i + 1;
            }
        }
    }

    public int GetFirst()
    {
        for (int i = 0; i < 9; i++)
        {
            if (((_possibilities >> i) & 1) > 0) return i + 1;
        }

        return 0;
    }

    public override int GetHashCode()
    {
        // ReSharper disable once NonReadonlyMemberInGetHashCode
        return _possibilities;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not BitPossibilities p) return false;
        return p._possibilities == _possibilities;
    }

    public override string ToString()
    {
        string result = "[";
        for (int i = 1; i <= 9; i++)
        {
            if (Peek(i)) result += i + ", ";
        }

        if (result.Length != 1) result = result[..^2];
        return result + "]";
    }
}