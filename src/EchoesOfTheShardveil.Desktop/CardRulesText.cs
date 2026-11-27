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
    private static readonly Lazy<AbilityTextCatalog> Abilities = new(LoadAbilities);

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
                var reminder = text.ReminderText.Replace("{startingHealth}", MatchFormats.Active.StartingHealth.ToString(CultureInfo.InvariantCulture),
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
    /// <param name="card">The card to summarize.</param>
    /// <returns>The keyword names, or a short description of the card's effects.</returns>
    public static string Summarize(CardDefinition card)
    {
        if (card.Keywords != CardKeyword.None)
        {
            return string.Join(", ", Keywords.Where(card.HasKeyword)
                .Select(keyword => Catalog.Value[keyword.ToString()].Name));
        }

        return string.Join(" / ", card.Abilities.Select(ability =>
            ExpandAbilityText(Abilities.Value.Actions[ability.Action].ShortText, ability)));
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
        var catalog = Abilities.Value;
        var trigger = ExpandAbilityText(catalog.Triggers[ability.Trigger], ability);
        var action = catalog.Actions[ability.Action];
        var template = ability.Amount == 1
            ? action.SingularReminderText ?? action.ReminderText
            : action.ReminderText;
        return $"{trigger} - {ExpandAbilityText(template, ability)}";
    }

    /// <summary>Replaces the supported tokens in an ability display template.</summary>
    /// <param name="template">The text from the ability catalog.</param>
    /// <param name="ability">The ability whose values are shown.</param>
    /// <returns>The formatted text.</returns>
    private static string ExpandAbilityText(string template, CardAbility ability) =>
        template.Replace("{amount}", ability.Amount.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{cost}", ability.Cost.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{target}", Abilities.Value.Targets[ability.Target], StringComparison.Ordinal)
            .Replace("{startingHealth}", MatchFormats.Active.StartingHealth.ToString(CultureInfo.InvariantCulture),
                StringComparison.Ordinal);

    /// <summary>Reads ability display text and verifies that each engine identifier has a label or template.</summary>
    /// <returns>The complete ability catalog.</returns>
    private static AbilityTextCatalog LoadAbilities()
    {
        var path = AppDataJson.PathFor("Rule", "Ability.json");
        var catalog = AppDataJson.Load<AbilityTextCatalog>(path);
        if (catalog.Triggers is null || catalog.Targets is null || catalog.Actions is null ||
            Enum.GetValues<AbilityTrigger>().Any(trigger =>
                !catalog.Triggers.TryGetValue(trigger, out var text) || string.IsNullOrWhiteSpace(text)) ||
            Enum.GetValues<AbilityTarget>().Any(target =>
                !catalog.Targets.TryGetValue(target, out var text) || string.IsNullOrWhiteSpace(text)) ||
            Enum.GetValues<AbilityAction>().Any(action =>
                !catalog.Actions.TryGetValue(action, out var text) || text is null ||
                string.IsNullOrWhiteSpace(text.ReminderText) || string.IsNullOrWhiteSpace(text.ShortText)))
        {
            throw new InvalidDataException($"'{path}' must provide display text for every ability trigger, target and action.");
        }

        return catalog;
    }

    /// <summary>The ability display text, indexed by engine identifiers.</summary>
    /// <param name="Triggers">Labels for when an effect resolves.</param>
    /// <param name="Targets">Phrases for what an effect applies to.</param>
    /// <param name="Actions">Templates for what an effect does.</param>
    private sealed record AbilityTextCatalog(
        Dictionary<AbilityTrigger, string> Triggers,
        Dictionary<AbilityTarget, string> Targets,
        Dictionary<AbilityAction, AbilityActionText> Actions);

    /// <summary>The full and condensed descriptions of an action.</summary>
    /// <param name="ReminderText">The full description shown in the inspector.</param>
    /// <param name="ShortText">The condensed description shown on a card face.</param>
    /// <param name="SingularReminderText">Optional full description for an amount of one.</param>
    private sealed record AbilityActionText(string ReminderText, string ShortText, string? SingularReminderText = null);
}
