namespace TheCards.Core;

/// <summary>Defines a card independently of its state in a duel.</summary>
public sealed record CardDefinition(string Id, string Name)
{
    public CardType Type { get; init; } = CardType.Unit;
    public int Cost { get; init; }
    public int Power { get; init; }
    public int Health { get; init; }
    public string? Subtype { get; init; }
    public string? Aspect { get; init; }
    public CardKeyword Keywords { get; init; }
    public IReadOnlyList<CardAbility> Abilities { get; init; } = [];
}

public enum CardType { Leader, Unit, Spell, Relic }

[Flags]
public enum CardKeyword
{
    None = 0,
    Rush = 1,
    Vigilant = 2,
    Sentinel = 4,
    Elusive = 8,
    Pierce = 16,
    Drain = 32,
}

public enum AbilityTrigger { Spell, Arrival, LastBreath, StartTurn, EndTurn, Activated }
public enum AbilityTarget { Controller, ChosenUnit, FriendlyUnit, RandomFriendlyUnit }
public enum AbilityAction { Damage, HealLeader, Draw, Ready, IncreasePower }

/// <summary>Describes a card effect and its optional activation cost.</summary>
public sealed record CardAbility(
    AbilityTrigger Trigger, AbilityTarget Target, AbilityAction Action, int Amount = 1, int Cost = 0);
