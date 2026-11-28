// 261127_code
// 261127_documentation

using System.Globalization;
using EchoesOfTheShardveil.Core;

namespace EchoesOfTheShardveil.Desktop;

/// <summary>Fills numeric match rules into player-facing text from AppData.</summary>
internal sealed class FormatText
{
    private readonly (string Token, int Value)[] tokens;

    /// <summary>Captures the rule values used by the text.</summary>
    /// <param name="format">The active duel format.</param>
    public FormatText(DuelFormat format)
    {
        tokens =
        [
            ("{startingHealth}", format.StartingHealth),
            ("{deckSize}", format.DeckSize),
            ("{maxCopiesPerName}", format.MaxCopiesPerName),
            ("{openingHandSize}", format.OpeningHandSize),
            ("{unitSlots}", format.UnitSlots),
            ("{relicSlots}", format.RelicSlots),
            ("{handLimit}", format.HandLimit),
            ("{wellLimit}", format.WellLimit),
            ("{sparkEnergy}", format.SparkEnergy),
            ("{cardsDrawnPerTurn}", format.CardsDrawnPerTurn),
            ("{wellFeedsPerTurn}", format.WellFeedsPerTurn),
            ("{maxMulliganSwaps}", format.MaxMulliganSwaps),
        ];
    }

    /// <summary>Replaces supported rule placeholders with their values.</summary>
    /// <param name="text">The template to fill in.</param>
    /// <returns>The formatted text.</returns>
    public string Expand(string text) => tokens.Aggregate(text, (value, token) =>
        value.Replace(token.Token, token.Value.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal));
}
