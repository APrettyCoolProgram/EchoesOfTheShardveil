// 261127_code
// 261127_documentation

using System.Globalization;
using EchoesOfTheShardveil.Core;

namespace EchoesOfTheShardveil.Desktop;

/// <summary>Turns a card's rules data into player-facing text.</summary>
internal static class CardRulesText
{
    private static readonly CardKeyword[] Keywords = Enum.GetValues<CardKeyword>()
        .Where(keyword => keyword != CardKeyword.None).ToArray();
    private static readonly Lazy<Dictionary<string, KeywordText>> Catalog = new(LoadKeywords);

    /// <summary>Writes the full rules of a card for the inspector.</summary>
    /// <param name="card">The card to describe.</param>
    /// <returns>One sentence for each keyword, followed by one for each ability.</returns>
    public static IEnumerable<string> Describe(CardDefinition card)
    {
        foreach (var keyword in Keywords)
        {
            if (card.HasKeyword(keyword))
            {
                var text = Catalog.Value[keyword.ToString()];
                var reminder = text.ReminderText.Replace("{startingHealth}", MatchFormats.Alpha.StartingHealth.ToString(CultureInfo.InvariantCulture),
                    StringComparison.Ordinal);
                yield return $"{text.Name} - {reminder}";
            }
        }

        foreach (var ability in card.Abilities)
        {
            yield return AbilityRule(ability);
        }
    }

    /// <summary>Writes the short text printed on a card face.</summary>
    /// <remarks>
    /// The wording is tailored to the starter cards and does not reflect every trigger or amount.
    /// Use <see cref="Describe"/> for accurate rules.
    /// </remarks>
    /// <param name="card">The card to summarize.</param>
    /// <returns>The keywords, or a short note on the first ability, or an empty string.</returns>
    public static string Summarize(CardDefinition card)
    {
        if (card.Keywords != CardKeyword.None)
        {
            return string.Join(", ", Keywords.Where(card.HasKeyword)
                .Select(keyword => Catalog.Value[keyword.ToString()].Name));
        }
        if (card.Abilities.Count == 0)
        {
            return "";
        }

        var ability = card.Abilities[0];
        return ability.Action switch
        {
            AbilityAction.Damage => $"Deal {ability.Amount} damage",
            AbilityAction.HealLeader => $"Arrival: heal {ability.Amount}",
            AbilityAction.Draw => "Last Breath: draw",
            AbilityAction.Ready => "Ready a unit; draw",
            AbilityAction.IncreasePower => "Start: +1 Power",
            _ => "",
        };
    }

    /// <summary>Reads the display catalog and checks that every engine keyword has an entry.</summary>
    /// <returns>The catalog keyed by the engine keyword ID.</returns>
    private static Dictionary<string, KeywordText> LoadKeywords()
    {
        var path = AppDataJson.PathFor("Rule", "Keyword.json");
        var catalog = AppDataJson.Load<Dictionary<string, KeywordText>>(path);
        foreach (var keyword in Keywords)
        {
            if (!catalog.TryGetValue(keyword.ToString(), out var text) || text is null ||
                string.IsNullOrWhiteSpace(text.Name) || string.IsNullOrWhiteSpace(text.ReminderText))
            {
                throw new InvalidDataException($"'{path}' is missing display text for '{keyword}'.");
            }
        }
        return catalog;
    }

    /// <summary>The player-facing text for one keyword.</summary>
    /// <param name="Name">The display name.</param>
    /// <param name="ReminderText">The rule reminder shown in the inspector.</param>
    private sealed record KeywordText(string Name, string ReminderText);

    /// <summary>Explains an ability, including when it resolves.</summary>
    /// <param name="ability">The ability.</param>
    /// <returns>One line of rules text.</returns>
    private static string AbilityRule(CardAbility ability)
    {
        var trigger = ability.Trigger switch
        {
            AbilityTrigger.Arrival => "Arrival",
            AbilityTrigger.LastBreath => "Last Breath",
            AbilityTrigger.StartTurn => "Start of your turn",
            AbilityTrigger.EndTurn => "End of your turn",
            AbilityTrigger.Activated => $"Activate for {ability.Cost} energy",
            _ => "On play",
        };
        var target = ability.Target switch
        {
            AbilityTarget.ChosenUnit => "a chosen Unit",
            AbilityTarget.FriendlyUnit => "a friendly Unit",
            AbilityTarget.RandomFriendlyUnit => "a random friendly Unit",
            _ => "your Hero",
        };
        var amount = ability.Amount;
        var action = ability.Action switch
        {
            AbilityAction.Damage => $"Deal {amount} damage to {target}.",
            AbilityAction.HealLeader => $"Restore {amount} Health to your Hero, up to {MatchFormats.Alpha.StartingHealth}.",
            AbilityAction.Draw => amount == 1 ? "Draw a card." : $"Draw {amount} cards.",
            AbilityAction.Ready => $"Ready {target}.",
            AbilityAction.IncreasePower => $"Give {target} +{amount} Power permanently.",
            _ => "",
        };
        return $"{trigger} - {action}";
    }
}
