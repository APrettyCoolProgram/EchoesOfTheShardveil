// 261127_code
// 261127_documentation

using EchoesOfTheShardveil.Core;

namespace EchoesOfTheShardveil.Desktop;

/// <summary>Exposes the active desktop match format.</summary>
internal static class MatchFormats
{
    private static readonly Lazy<DuelFormat> AlphaFormat = new(LoadAlpha);

    /// <summary>Gets the limits used for Alpha matches and deckbuilding.</summary>
    public static DuelFormat Alpha => AlphaFormat.Value;

    /// <summary>Loads the Alpha match limits from AppData.</summary>
    /// <returns>The validated duel format.</returns>
    private static DuelFormat LoadAlpha()
    {
        var path = AppDataJson.PathFor("Rule", "AlphaFormat.json");
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
