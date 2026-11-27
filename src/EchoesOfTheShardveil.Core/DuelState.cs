namespace EchoesOfTheShardveil.Core;

/// <summary>The step of a duel that is waiting to be resolved.</summary>
public enum DuelPhase
{
    /// <summary>Players choose their opening hands.</summary>
    Mulligan,
    /// <summary>The active player refills the Well, readies Units and resolves start-of-turn abilities.</summary>
    Ready,
    /// <summary>The active player draws a card.</summary>
    Draw,
    /// <summary>The active player feeds the Well, uses the Spark and plays cards.</summary>
    Main,
    /// <summary>The active player attacks.</summary>
    Battle,
    /// <summary>The active player discards down to the hand limit.</summary>
    End,
    /// <summary>The duel is over.</summary>
    Finished,
}

/// <summary>The result of a duel.</summary>
public enum DuelOutcome
{
    /// <summary>Neither player has lost.</summary>
    Ongoing,
    /// <summary>The player at index 0 won.</summary>
    PlayerOneWins,
    /// <summary>The player at index 1 won.</summary>
    PlayerTwoWins,
    /// <summary>Both players lost at the same time.</summary>
    Draw,
}

/// <summary>Addresses a Unit slot on either side.</summary>
/// <param name="Player">The index of the player who controls the slot, 0 or 1.</param>
/// <param name="Slot">The zero-based Unit slot.</param>
public readonly record struct UnitTarget(int Player, int Slot);

/// <summary>A legal destination and optional target for a card played from hand.</summary>
/// <param name="BoardSlot">The Unit or Relic slot the card goes to, or <see langword="null"/> for a Spell.</param>
/// <param name="Target">The Unit the card's ability applies to, if it needs one.</param>
public readonly record struct CardPlay(int? BoardSlot, UnitTarget? Target);

/// <summary>Public view of a Unit, including damage and exhaustion.</summary>
/// <param name="Card">The card the Unit was played from.</param>
/// <param name="Power">The current Power, including permanent increases.</param>
/// <param name="Damage">The damage the Unit has taken.</param>
/// <param name="Exhausted">Whether the Unit is exhausted.</param>
/// <param name="CanAttack">Whether the Unit can be declared as an attacker now.</param>
/// <param name="CanIntercept">Whether the Unit can intercept the pending attack.</param>
public sealed record UnitView(CardDefinition Card, int Power, int Damage, bool Exhausted, bool CanAttack, bool CanIntercept);

/// <summary>Public zones and counts. Only the viewing player's hand is included.</summary>
/// <param name="Leader">The player's Leader card.</param>
/// <param name="Health">The player's remaining Health.</param>
/// <param name="DeckCount">The number of cards left in the deck.</param>
/// <param name="HandCount">The number of cards in hand.</param>
/// <param name="Hand">The cards in hand, or <see langword="null"/> when the viewer is the other player.</param>
/// <param name="Shards">The number of shards in the Well.</param>
/// <param name="ReadyShards">The number of shards that can still pay for something this turn.</param>
/// <param name="TemporaryEnergy">Extra energy that expires at the end of the turn.</param>
/// <param name="HasSpark">Whether the player still has the Spark.</param>
/// <param name="CanFeedWell">Whether the player can add a shard to the Well now.</param>
/// <param name="Units">The Unit slots, with <see langword="null"/> for each empty slot.</param>
/// <param name="Relics">The Relic slots, with <see langword="null"/> for each empty slot.</param>
/// <param name="Discard">The discard pile, oldest first.</param>
public sealed record PlayerView(CardDefinition Leader, int Health, int DeckCount, int HandCount,
    IReadOnlyList<CardDefinition>? Hand, int Shards, int ReadyShards, int TemporaryEnergy,
    bool HasSpark, bool CanFeedWell, IReadOnlyList<UnitView?> Units,
    IReadOnlyList<CardDefinition?> Relics, IReadOnlyList<CardDefinition> Discard);

/// <summary>An attack waiting for the defending player's choice.</summary>
/// <param name="Attacker">The attacking Unit.</param>
/// <param name="Target">The attacked Unit, or <see langword="null"/> for an attack on the Leader.</param>
public sealed record AttackView(UnitTarget Attacker, UnitTarget? Target);
