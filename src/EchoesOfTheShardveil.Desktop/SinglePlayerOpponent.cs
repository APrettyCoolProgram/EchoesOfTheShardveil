using EchoesOfTheShardveil.Core;

namespace EchoesOfTheShardveil.Desktop;

/// <summary>Plays one legal action for the Alpha.02 computer opponent.</summary>
internal static class SinglePlayerOpponent
{
    public const int Side = 1;

    public static string TakeStep(Duel duel)
    {
        var own = duel.View(Side, Side);
        var enemy = duel.View(0, Side);
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
                var energy = own.ReadyShards + own.TemporaryEnergy;
                var plays = Enumerable.Range(0, hand.Count).OrderByDescending(index => hand[index].Cost);
                foreach (var slot in plays)
                {
                    var card = hand[slot];
                    if (card.Cost > energy + (own.HasSpark ? 1 : 0)
                        || (card.Type == CardType.Unit && own.Units.All(unit => unit is not null))
                        || (card.Type == CardType.Relic && own.Relics.All(relic => relic is not null)))
                    {
                        continue;
                    }

                    UnitTarget? target = null;
                    var targetType = ChosenTarget(card);
                    if (targetType is not null)
                    {
                        var friendly = targetType == AbilityTarget.FriendlyUnit;
                        var selected = FindUnit(friendly ? own : enemy, _ => true);
                        if (selected < 0)
                        {
                            continue;
                        }
                        target = new(friendly ? Side : 0, selected);
                    }

                    if (card.Cost > energy)
                    {
                        duel.UseSpark(Side);
                    }
                    duel.Play(Side, slot, target);
                    return $"Opponent played {card.Name}.";
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
            case DuelPhase.End when hand.Count > Duel.HandLimit:
                duel.DiscardToLimit(Side, hand.Count - 1);
                return "Opponent discarded to the hand limit.";
        }

        duel.Advance(Side);
        return duel.ActivePlayer == 0 ? "Your turn begins." : "";
    }

    public static AbilityTarget? ChosenTarget(CardDefinition card)
    {
        var trigger = card.Type == CardType.Spell ? AbilityTrigger.Spell : AbilityTrigger.Arrival;
        return card.Abilities.FirstOrDefault(ability => ability.Trigger == trigger &&
            ability.Target == AbilityTarget.FriendlyUnit)?.Target
            ?? card.Abilities.FirstOrDefault(ability => ability.Trigger == trigger &&
                ability.Target == AbilityTarget.ChosenUnit)?.Target;
    }

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
