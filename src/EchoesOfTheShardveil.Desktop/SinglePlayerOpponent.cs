// 261127_code
// 261127_documentation

using EchoesOfTheShardveil.Core;

namespace EchoesOfTheShardveil.Desktop;

/// <summary>Plays one legal action for the Alpha.02 computer opponent.</summary>
internal static class SinglePlayerOpponent
{
    /// <summary>The player index the computer controls.</summary>
    public const int Side = 1;

    /// <summary>The player index of the person it plays against.</summary>
    public const int HumanSide = 0;

    /// <summary>Takes the computer's next single action, so the player can follow along.</summary>
    /// <param name="duel">A duel waiting for the computer.</param>
    /// <returns>A message describing the action, or an empty string when there is nothing worth showing.</returns>
    public static string TakeStep(Duel duel)
    {
        var own = duel.View(Side, Side);
        var enemy = duel.View(HumanSide, Side);
        var hand = own.Hand!;
        if (duel.PendingAttack is not null)
        {
            var defender = FindUnit(own, unit => unit.CanIntercept);
            duel.Defend(Side, defender >= 0 ? defender : null);
            return defender >= 0 ? $"{own.Units[defender]!.Card.Name} intercepted." : "Opponent took the attack.";
        }

        switch (duel.Phase)
        {
            case DuelPhase.Main:
                if (own.CanFeedWell && hand.Count > 1)
                {
                    var shard = Enumerable.Range(0, hand.Count).MaxBy(slot => hand[slot].Cost);
                    duel.FeedWell(Side, shard);
                    return "Opponent fed the Well.";
                }
                if (TryPlayCard(duel, own, enemy) is { } played)
                {
                    return $"Opponent played {played.Name}.";
                }
                break;
            case DuelPhase.Battle:
                var attacker = FindUnit(own, unit => unit.CanAttack);
                if (attacker >= 0)
                {
                    duel.Attack(Side, attacker);
                    return $"{own.Units[attacker]!.Card.Name} attacks your Hero.";
                }
                break;
            case DuelPhase.End when hand.Count > duel.Format.HandLimit:
                duel.DiscardToLimit(Side, hand.Count - 1);
                return "Opponent discarded to the hand limit.";
        }

        duel.Advance(Side);
        return duel.ActivePlayer == HumanSide ? "Your turn begins." : "";
    }

    /// <summary>Plays the most expensive card that is affordable and has somewhere to go.</summary>
    /// <param name="duel">The duel in the computer's Main phase.</param>
    /// <param name="own">The computer's side.</param>
    /// <param name="enemy">The human's side.</param>
    /// <returns>The card played, or <see langword="null"/> if none could be.</returns>
    private static CardDefinition? TryPlayCard(Duel duel, PlayerView own, PlayerView enemy)
    {
        var hand = own.Hand!;
        var energy = own.ReadyShards + own.TemporaryEnergy;
        foreach (var slot in Enumerable.Range(0, hand.Count).OrderByDescending(index => hand[index].Cost))
        {
            var card = hand[slot];
            if (card.Cost > energy + (own.HasSpark ? 1 : 0)
                || (card.Type == CardType.Unit && own.Units.All(unit => unit is not null))
                || (card.Type == CardType.Relic && own.Relics.All(relic => relic is not null)))
            {
                continue;
            }

            UnitTarget? target = null;
            if (RequiredTarget(card) is { } targetType)
            {
                var friendly = targetType == AbilityTarget.FriendlyUnit;
                var selected = FindUnit(friendly ? own : enemy, _ => true);
                if (selected < 0)
                {
                    continue;
                }
                target = new(friendly ? Side : HumanSide, selected);
            }

            if (card.Cost > energy)
            {
                duel.UseSpark(Side);
            }
            duel.Play(Side, slot, target);
            return card;
        }
        return null;
    }

    /// <summary>Gets the kind of Unit a card needs as a target when played. A friendly-only requirement wins.</summary>
    /// <param name="card">The card.</param>
    /// <returns>The target kind, or <see langword="null"/> if none is needed.</returns>
    private static AbilityTarget? RequiredTarget(CardDefinition card)
    {
        var abilities = card.GetPlayAbilities();
        if (abilities.Any(ability => ability.Target == AbilityTarget.FriendlyUnit))
        {
            return AbilityTarget.FriendlyUnit;
        }
        return abilities.Any(ability => ability.Target == AbilityTarget.ChosenUnit) ? AbilityTarget.ChosenUnit : null;
    }

    /// <summary>Finds the first Unit that matches.</summary>
    /// <param name="view">The side to search.</param>
    /// <param name="predicate">The condition.</param>
    /// <returns>The Unit slot, or -1.</returns>
    private static int FindUnit(PlayerView view, Func<UnitView, bool> predicate)
    {
        for (var slot = 0; slot < view.Units.Count; slot++)
        {
            if (view.Units[slot] is { } unit && predicate(unit))
            {
                return slot;
            }
        }
        return -1;
    }
}
