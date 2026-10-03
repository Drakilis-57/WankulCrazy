using System;
using System.Collections.Generic;

namespace WankulCrazy.Duel.Engine;

public interface IRandom
{
    int Next(int minInclusive, int maxExclusive);
    int Next(int maxExclusive);
    void Shuffle<T>(IList<T> list);
}

public sealed class SystemRandomAdapter : IRandom
{
    private readonly Random _random;

    public SystemRandomAdapter()
    {
        _random = new Random();
    }

    public SystemRandomAdapter(int seed)
    {
        _random = new Random(seed);
    }

    public int Next(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);

    public int Next(int maxExclusive) => _random.Next(maxExclusive);

    public void Shuffle<T>(IList<T> list)
    {
        if (list == null) return;
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = _random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
