// 261127_code
// 261127_documentation

namespace EchoesOfTheShardveil.Desktop;

/// <summary>Help text for the board's badges and card counters.</summary>
/// <param name="Health">The player's Health badge.</param>
/// <param name="Well">The player's Well badge.</param>
/// <param name="OpponentHealth">The opponent's Health badge.</param>
/// <param name="OpponentWell">The opponent's Well badge.</param>
/// <param name="Discard">The discard pile counter.</param>
/// <param name="Deck">The deck counter.</param>
/// <param name="Removed">The removed cards counter.</param>
internal sealed record BoardHelp(Tooltip Health, Tooltip Well, Tooltip OpponentHealth, Tooltip OpponentWell,
    Tooltip Discard, Tooltip Deck, Tooltip Removed)
{
    private static readonly Lazy<BoardHelp> Text = new(Load);

    /// <summary>Gets the board help with the active format's values filled in.</summary>
    public static BoardHelp Current => Text.Value;

    /// <summary>Loads and formats the shipped board help catalog.</summary>
    /// <returns>The validated tooltips.</returns>
    private static BoardHelp Load()
    {
        var path = AppDataJson.PathFor("Rule", "BoardHelp.json");
        var help = AppDataJson.Load<BoardHelp>(path);
        var formatText = new FormatText(MatchFormats.Active);
        Tooltip Resolve(Tooltip tooltip)
        {
            if (tooltip is null || string.IsNullOrWhiteSpace(tooltip.Heading) ||
                string.IsNullOrWhiteSpace(tooltip.Body))
            {
                throw new InvalidDataException($"'{path}' must provide a heading and body for every tooltip.");
            }
            return new Tooltip(formatText.Expand(tooltip.Heading), formatText.Expand(tooltip.Body));
        }

        return new BoardHelp(Resolve(help.Health), Resolve(help.Well), Resolve(help.OpponentHealth),
            Resolve(help.OpponentWell), Resolve(help.Discard), Resolve(help.Deck), Resolve(help.Removed));
    }
}

/// <summary>Text shown when hovering over a badge or counter.</summary>
/// <param name="Heading">The short title.</param>
/// <param name="Body">The explanatory text.</param>
internal sealed record Tooltip(string Heading, string Body);
