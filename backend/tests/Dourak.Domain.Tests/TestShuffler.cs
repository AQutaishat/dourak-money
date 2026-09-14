using Dourak.Domain.Entities;

namespace Dourak.Domain.Tests;

/// <summary>Deterministic reverse "shuffle" used only to assert draw plumbing in tests.</summary>
public class ReverseShuffler : IRandomShuffler
{
    public IReadOnlyList<int> Shuffle(IReadOnlyList<int> items) => items.Reverse().ToList();
}
