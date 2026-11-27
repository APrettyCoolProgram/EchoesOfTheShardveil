// 261127_code
// 261127_documentation

using EchoesOfTheShardveil.Core;

namespace EchoesOfTheShardveil.Desktop;

/// <summary>Exposes the active desktop match format.</summary>
internal static class MatchFormats
{
    private static readonly Lazy<DuelFormat> CurrentFormat = new(Load);

    /// <summary>Gets the limits used by the current matchup and deckbuilder.</summary>
    public static DuelFormat Active => CurrentFormat.Value;

    /// <summary>Loads the configured match limits from AppData.</summary>
    /// <returns>The validated duel format.</returns>
    private static DuelFormat Load()
    {
        var path = AppDataJson.ResolveReference(MatchSetup.Current.Format);
        try
        {
            return AppDataJson.Load<DuelFormat>(path);
        }
        catch (ArgumentOutOfRangeException error)
        {
            throw new InvalidDataException($"Invalid match limit in '{path}': {error.Message}", error);
        }
    }
}
