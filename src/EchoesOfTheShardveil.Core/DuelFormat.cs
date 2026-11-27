// 261127_code
// 261127_documentation

namespace EchoesOfTheShardveil.Core;

/// <summary>Defines the deck and Health limits for a duel.</summary>
public sealed class DuelFormat
{
    /// <summary>Creates a format with limits that can support an opening hand.</summary>
    /// <param name="startingHealth">Starting Health and the healing cap.</param>
    /// <param name="deckSize">The required number of cards per deck.</param>
    /// <param name="maxCopiesPerName">The maximum number of copies of a card name per deck.</param>
    /// <exception cref="ArgumentOutOfRangeException">A limit is too low to start a duel.</exception>
    public DuelFormat(int startingHealth, int deckSize, int maxCopiesPerName)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(startingHealth);
        ArgumentOutOfRangeException.ThrowIfLessThan(deckSize, Duel.OpeningHandSize);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxCopiesPerName);
        StartingHealth = startingHealth;
        DeckSize = deckSize;
        MaxCopiesPerName = maxCopiesPerName;
    }

    /// <summary>Gets the starting Health and healing cap.</summary>
    public int StartingHealth { get; }

    /// <summary>Gets the required number of cards per deck.</summary>
    public int DeckSize { get; }

    /// <summary>Gets the copy limit for each card name.</summary>
    public int MaxCopiesPerName { get; }
}
