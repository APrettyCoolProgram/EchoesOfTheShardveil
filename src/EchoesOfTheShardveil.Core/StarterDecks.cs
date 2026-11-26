namespace EchoesOfTheShardveil.Core;

/// <summary>Provides two legal, experimental decks for Alpha.02.</summary>
public static class StarterDecks
{
    private static readonly CardDefinition[] Cards =
    [
        Unit("ember-pup", "Ember Pup", 1, 2, 1, "Beast", CardKeyword.Rush),
        Unit("lantern-squire", "Lantern Squire", 2, 2, 3, "Knight", CardKeyword.Sentinel),
        Unit("mistwing-scout", "Mistwing Scout", 2, 2, 2, "Bird", CardKeyword.Elusive),
        Unit("bramble-warden", "Bramble Warden", 3, 2, 4, "Plant") with
        {
            Abilities = [new(AbilityTrigger.Arrival, AbilityTarget.Controller, AbilityAction.HealLeader, 3)],
        },
        Unit("wandering-oathsworn", "Wandering Oathsworn", 4, 4, 4, "Knight", CardKeyword.Vigilant),
        Unit("gravebloom", "Gravebloom", 3, 3, 2, "Spirit") with
        {
            Abilities = [new(AbilityTrigger.LastBreath, AbilityTarget.Controller, AbilityAction.Draw)],
        },
        Unit("tusked-charger", "Tusked Charger", 5, 5, 4, "Beast", CardKeyword.Pierce),
        new("spark-bolt", "Spark Bolt")
        {
            Type = CardType.Spell, Cost = 2,
            Abilities = [new(AbilityTrigger.Spell, AbilityTarget.ChosenUnit, AbilityAction.Damage, 3)],
        },
        new("second-wind", "Second Wind")
        {
            Type = CardType.Spell, Cost = 1,
            Abilities =
            [
                new(AbilityTrigger.Spell, AbilityTarget.FriendlyUnit, AbilityAction.Ready),
                new(AbilityTrigger.Spell, AbilityTarget.Controller, AbilityAction.Draw),
            ],
        },
        new("beacon-of-tides", "Beacon of Tides")
        {
            Type = CardType.Relic, Cost = 3,
            Abilities = [new(AbilityTrigger.StartTurn, AbilityTarget.RandomFriendlyUnit, AbilityAction.IncreasePower)],
        },
        Unit("gate-watch", "Gate Watch", 1, 1, 2, "Knight"),
        Unit("mire-runner", "Mire Runner", 2, 3, 2, "Beast"),
        Unit("hollow-bat", "Hollow Bat", 3, 3, 3, "Beast", CardKeyword.Drain),
        Unit("rampart-golem", "Rampart Golem", 4, 4, 5, "Construct"),
    ];

    /// <summary>Builds a deck with the same test pool and a different card mix for each side.</summary>
    public static IReadOnlyList<CardDefinition> Create(bool secondDeck = false)
    {
        var reduced = secondDeck ? new[] { "lantern-squire", "bramble-warden" }
            : new[] { "mistwing-scout", "tusked-charger" };
        return Cards.SelectMany(card => Enumerable.Repeat(card, reduced.Contains(card.Id) ? 2 : 3)).ToArray();
    }

    private static CardDefinition Unit(string id, string name, int cost, int power, int health,
        string subtype, CardKeyword keywords = CardKeyword.None) =>
        new(id, name) { Cost = cost, Power = power, Health = health, Subtype = subtype, Keywords = keywords };
}
