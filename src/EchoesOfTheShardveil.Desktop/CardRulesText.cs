// 261127_code
// 261127_documentation

using EchoesOfTheShardveil.Core;

namespace EchoesOfTheShardveil.Desktop;

/// <summary>Turns a card's rules data into player-facing text.</summary>
internal static class CardRulesText
{
    /// <summary>Writes the full rules of a card for the inspector.</summary>
    /// <param name="card">The card to describe.</param>
    /// <returns>One sentence for each keyword, followed by one for each ability.</returns>
    public static IEnumerable<string> Describe(CardDefinition card)
    {
        foreach (var keyword in Enum.GetValues<CardKeyword>())
        {
            if (keyword != CardKeyword.None && card.HasKeyword(keyword))
            {
                yield return $"{keyword} - {KeywordRule(keyword)}";
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
            return card.Keywords.ToString();
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

    /// <summary>Explains a keyword.</summary>
    /// <param name="keyword">A single keyword.</param>
    /// <returns>The rule, or an empty string.</returns>
    private static string KeywordRule(CardKeyword keyword) => keyword switch
    {
        CardKeyword.Rush => "Can attack on the turn it enters play.",
        CardKeyword.Vigilant => "Stays ready after attacking.",
        CardKeyword.Sentinel => "Can intercept while exhausted.",
        CardKeyword.Elusive => "Only Elusive Units can intercept its attacks.",
        CardKeyword.Pierce => "Excess combat damage hits the Opponent.",
        CardKeyword.Drain => $"Restores your Hero by the damage dealt, up to {Duel.StartingHealth} Health.",
        _ => "",
    };

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
            AbilityAction.HealLeader => $"Restore {amount} Health to your Hero, up to {Duel.StartingHealth}.",
            AbilityAction.Draw => amount == 1 ? "Draw a card." : $"Draw {amount} cards.",
            AbilityAction.Ready => $"Ready {target}.",
            AbilityAction.IncreasePower => $"Give {target} +{amount} Power permanently.",
            _ => "",
        };
        return $"{trigger} - {action}";
    }
}
