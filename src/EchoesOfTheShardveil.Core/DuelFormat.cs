// 261127_code
// 261127_documentation

namespace EchoesOfTheShardveil.Core;

/// <summary>Defines the setup and limits for a duel.</summary>
public sealed class DuelFormat
{
    /// <summary>Creates a format with limits that can support an opening hand.</summary>
    /// <param name="startingHealth">Starting Health and the healing cap.</param>
    /// <param name="deckSize">The required number of cards per deck.</param>
    /// <param name="maxCopiesPerName">The maximum number of copies of a card name per deck.</param>
    /// <param name="openingHandSize">The number of cards dealt before mulligans.</param>
    /// <param name="handLimit">The maximum hand size at the end of a turn.</param>
    /// <param name="secondPlayerGetsSpark">Whether the player going second receives a Spark.</param>
    /// <param name="unitSlots">The number of Unit slots per side.</param>
    /// <param name="relicSlots">The number of Relic slots per side.</param>
    /// <exception cref="ArgumentOutOfRangeException">A limit is invalid or exceeds the deck size.</exception>
    public DuelFormat(int startingHealth, int deckSize, int maxCopiesPerName,
        int openingHandSize = Duel.OpeningHandSize, int handLimit = Duel.HandLimit, bool secondPlayerGetsSpark = true,
        int unitSlots = Duel.UnitSlots, int relicSlots = Duel.RelicSlots)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(startingHealth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(deckSize);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(openingHandSize);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(openingHandSize, deckSize);
        ArgumentOutOfRangeException.ThrowIfNegative(handLimit);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxCopiesPerName);
        ArgumentOutOfRangeException.ThrowIfNegative(unitSlots);
        ArgumentOutOfRangeException.ThrowIfNegative(relicSlots);
        StartingHealth = startingHealth;
        DeckSize = deckSize;
        MaxCopiesPerName = maxCopiesPerName;
        OpeningHandSize = openingHandSize;
        HandLimit = handLimit;
        SecondPlayerGetsSpark = secondPlayerGetsSpark;
        UnitSlots = unitSlots;
        RelicSlots = relicSlots;
    }

    /// <summary>Gets the starting Health and healing cap.</summary>
    public int StartingHealth { get; }

    /// <summary>Gets the required number of cards per deck.</summary>
    public int DeckSize { get; }

    /// <summary>Gets the copy limit for each card name.</summary>
    public int MaxCopiesPerName { get; }

    /// <summary>Gets the number of cards dealt before mulligans.</summary>
    public int OpeningHandSize { get; }

    /// <summary>Gets the maximum hand size at the end of a turn.</summary>
    public int HandLimit { get; }

    /// <summary>Gets whether the player going second receives a Spark.</summary>
    public bool SecondPlayerGetsSpark { get; }

    /// <summary>Gets the number of Unit slots per side.</summary>
    public int UnitSlots { get; }

    /// <summary>Gets the number of Relic slots per side.</summary>
    public int RelicSlots { get; }
}
