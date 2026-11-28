// 261127_code
// 261127_documentation

namespace EchoesOfTheShardveil.Desktop;

/// <summary>Configures how the computer chooses among legal actions.</summary>
/// <param name="CardsToKeepWhenFeeding">The number of hand cards to keep after adding a shard.</param>
/// <param name="SpendSpark">Whether the computer may spend Spark to pay for a card.</param>
/// <param name="TargetPriority">How to choose among legal targets on the preferred side.</param>
internal sealed record OpponentProfile(int CardsToKeepWhenFeeding = 1, bool SpendSpark = true,
    OpponentTargetPriority TargetPriority = OpponentTargetPriority.FirstSlot)
{
    private static readonly Lazy<OpponentProfile> Loaded = new(Load);

    /// <summary>Gets the profile selected by the active matchup.</summary>
    public static OpponentProfile Current => Loaded.Value;

    /// <summary>Reads the configured profile or uses the original opponent preferences.</summary>
    /// <returns>The validated profile.</returns>
    private static OpponentProfile Load()
    {
        if (MatchSetup.Current.OpponentProfile is not { } reference)
        {
            return new OpponentProfile();
        }

        var path = AppDataJson.ResolveReference(reference);
        var profile = AppDataJson.Load<OpponentProfile>(path);
        if (profile.CardsToKeepWhenFeeding < 0)
        {
            throw new InvalidDataException($"'{path}' must set a nonnegative number of cards to keep when feeding.");
        }
        return profile;
    }
}

/// <summary>Preferences for choosing a Unit when playing a targeted card.</summary>
internal enum OpponentTargetPriority
{
    /// <summary>Choose the earliest legal slot.</summary>
    FirstSlot,
    /// <summary>Choose the legal Unit with the least remaining Health.</summary>
    LowestHealth,
    /// <summary>Choose the legal Unit with the most Power.</summary>
    HighestPower,
}
