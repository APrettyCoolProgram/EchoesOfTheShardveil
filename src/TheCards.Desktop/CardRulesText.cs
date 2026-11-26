using TheCards.Core;

namespace TheCards.Desktop;

/// <summary>Turns a card's rules data into player-facing text.</summary>
internal static class CardRulesText
{
    public static IEnumerable<string> Describe(CardDefinition card)
    {
        foreach (var keyword in Enum.GetValues<CardKeyword>())
        {
            if (keyword != CardKeyword.None && (card.Keywords & keyword) != 0)
            {
                yield return $"{keyword} - {KeywordRule(keyword)}";
            }
        }

        foreach (var ability in card.Abilities)
        {
            yield return AbilityRule(ability);
        }
    }

    private static string KeywordRule(CardKeyword keyword) => keyword switch
    {
        CardKeyword.Rush => "Can attack on the turn it enters play.",
        CardKeyword.Vigilant => "Stays ready after attacking.",
        CardKeyword.Sentinel => "Can intercept while exhausted.",
        CardKeyword.Elusive => "Only Elusive Units can intercept its attacks.",
        CardKeyword.Pierce => "Excess combat damage hits the opposing Leader.",
        CardKeyword.Drain => "Restores your Hero by the damage dealt, up to 20 Health.",
        _ => "",
    };

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
            AbilityAction.HealLeader => $"Restore {amount} Health to your Hero, up to 20.",
            AbilityAction.Draw => amount == 1 ? "Draw a card." : $"Draw {amount} cards.",
            AbilityAction.Ready => $"Ready {target}.",
            AbilityAction.IncreasePower => $"Give {target} +{amount} Power permanently.",
            _ => "",
        };
        return $"{trigger} - {action}";
    }
}
