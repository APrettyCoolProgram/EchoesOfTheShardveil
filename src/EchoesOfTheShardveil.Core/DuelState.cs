namespace EchoesOfTheShardveil.Core;

public enum DuelPhase { Mulligan, Ready, Draw, Main, Battle, End, Finished }
public enum DuelOutcome { Ongoing, PlayerOneWins, PlayerTwoWins, Draw }

/// <summary>Addresses a Unit slot on either side.</summary>
public readonly record struct UnitTarget(int Player, int Slot);

/// <summary>A legal destination and optional target for a card played from hand.</summary>
public readonly record struct CardPlay(int? BoardSlot, UnitTarget? Target);

/// <summary>Public view of a Unit, including damage and exhaustion.</summary>
public sealed record UnitView(CardDefinition Card, int Power, int Damage, bool Exhausted, bool CanAttack, bool CanIntercept);

/// <summary>Public zones and counts. Only the viewing player's hand is included.</summary>
public sealed record PlayerView(CardDefinition Leader, int Health, int DeckCount, int HandCount,
    IReadOnlyList<CardDefinition>? Hand, int Shards, int ReadyShards, int TemporaryEnergy,
    bool HasSpark, bool CanFeedWell, IReadOnlyList<UnitView?> Units,
    IReadOnlyList<CardDefinition?> Relics, IReadOnlyList<CardDefinition> Discard);

/// <summary>An attack waiting for the defending player's choice.</summary>
public sealed record AttackView(UnitTarget Attacker, UnitTarget? Target);
