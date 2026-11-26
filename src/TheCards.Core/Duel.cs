using System.Runtime.InteropServices;


namespace TheCards.Core;

/// <summary>Runs a deterministic two-player Alpha.02 duel when supplied with a seed.</summary>
public sealed class Duel
{
    public const int StartingHealth = 20;
    public const int DeckSize = 40;
    public const int OpeningHandSize = 5;
    public const int HandLimit = 10;
    public const int UnitSlots = 5;
    public const int RelicSlots = 2;
    public const int WellLimit = 10;

    private readonly Player[] players;
    private readonly Random random;
    private AttackView? pendingAttack;
    private readonly int firstPlayer;

    /// <summary>Validates and shuffles both decks, chooses the first player, and deals opening hands.</summary>
    public Duel(IEnumerable<CardDefinition> playerOneDeck, IEnumerable<CardDefinition> playerTwoDeck, int? seed = null)
    {
        random = seed is int value ? new Random(value) : new Random();
        players =
        [
            new(new("leader-one", "Leader One") { Type = CardType.Leader }, ValidateDeck(playerOneDeck)),
            new(new("leader-two", "Leader Two") { Type = CardType.Leader }, ValidateDeck(playerTwoDeck)),
        ];
        firstPlayer = random.Next(players.Length);
        ActivePlayer = firstPlayer;
        players[1 - firstPlayer].HasSpark = true;
        foreach (var player in players)
        {
            random.Shuffle(CollectionsMarshal.AsSpan(player.Deck));
            Draw(player, OpeningHandSize);
        }
    }

    public DuelPhase Phase { get; private set; } = DuelPhase.Mulligan;
    public DuelOutcome Outcome { get; private set; }
    public int ActivePlayer { get; private set; }
    public int Turn { get; private set; } = 1;
    public AttackView? PendingAttack => pendingAttack;

    /// <summary>Returns a snapshot, revealing only the viewer's hand and public information.</summary>
    public PlayerView View(int player, int viewer)
    {
        var owner = GetPlayer(player);
        GetPlayer(viewer);
        var units = owner.Units.Select(unit => unit is null ? null : new UnitView(unit.Card, unit.Power,
            unit.Damage, unit.Exhausted,
            Phase == DuelPhase.Battle && player == ActivePlayer && pendingAttack is null && CanAttack(unit),
            pendingAttack is { } attack && player != ActivePlayer && CanIntercept(unit, GetUnit(attack.Attacker)!)))
            .ToArray();
        return new(owner.Leader, owner.Health, owner.Deck.Count, owner.Hand.Count,
            player == viewer ? owner.Hand.ToArray() : null, owner.Well.Count, owner.ReadyShards,
            owner.TemporaryEnergy, owner.HasSpark, Phase == DuelPhase.Main && ActivePlayer == player &&
            !owner.FedWell && owner.Well.Count < WellLimit, units, owner.Relics.ToArray(), owner.Discard.ToArray());
    }

    /// <summary>Exchanges selected cards once during setup. An empty selection keeps the hand.</summary>
    public void Mulligan(int player, IEnumerable<int> handSlots)
    {
        Require(Phase == DuelPhase.Mulligan, "Mulligans are only available during setup.");
        var owner = GetPlayer(player);
        Require(!owner.MulliganComplete, "That player already chose their opening hand.");
        var slots = CheckedHandSlots(owner, handSlots);
        var returned = slots.Select(slot => owner.Hand[slot]).ToArray();
        random.Shuffle(returned);
        foreach (var slot in slots.OrderDescending())
        {
            owner.Hand.RemoveAt(slot);
        }
        owner.Deck.InsertRange(0, returned);
        Draw(owner, returned.Length);
        owner.MulliganComplete = true;
        if (players.All(p => p.MulliganComplete))
        {
            Phase = DuelPhase.Ready;
        }
    }

    /// <summary>Resolves the current phase and progresses the turn.</summary>
    public void Advance(int player)
    {
        var owner = RequireActive(player);
        switch (Phase)
        {
            case DuelPhase.Ready:
                owner.ReadyShards = owner.Well.Count;
                owner.FedWell = false;
                foreach (var unit in owner.Units.OfType<Unit>())
                {
                    unit.Exhausted = false;
                }
                ResolveBoardTriggers(player, AbilityTrigger.StartTurn);
                SetNextPhase(DuelPhase.Draw);
                break;
            case DuelPhase.Draw:
                if (Turn != 1 || player != firstPlayer)
                {
                    Draw(owner, 1);
                }
                SetNextPhase(DuelPhase.Main);
                break;
            case DuelPhase.Main:
                Phase = DuelPhase.Battle;
                break;
            case DuelPhase.Battle:
                Require(pendingAttack is null, "The defender must resolve the pending attack.");
                Phase = DuelPhase.End;
                ResolveBoardTriggers(player, AbilityTrigger.EndTurn);
                CheckOutcome();
                break;
            case DuelPhase.End:
                Require(owner.Hand.Count <= HandLimit, "Discard down to the hand limit first.");
                owner.ReadyShards = 0;
                owner.TemporaryEnergy = 0;
                ActivePlayer = 1 - ActivePlayer;
                Turn++;
                Phase = DuelPhase.Ready;
                break;
            default:
                throw new InvalidOperationException("The phase cannot advance.");
        }
    }

    /// <summary>Discards a selected card during the end phase when hand size exceeds the limit.</summary>
    public void DiscardToLimit(int player, int handSlot)
    {
        var owner = RequirePhase(player, DuelPhase.End);
        Require(owner.Hand.Count > HandLimit, "The hand is within the limit.");
        owner.Discard.Add(GetHandCard(owner, handSlot));
        owner.Hand.RemoveAt(handSlot);
    }

    /// <summary>Turns a selected card in hand into a ready shard.</summary>
    public void FeedWell(int player, int handSlot)
    {
        var owner = RequirePhase(player, DuelPhase.Main);
        Require(!owner.FedWell && owner.Well.Count < WellLimit, "The Well cannot receive another shard this turn.");
        owner.Well.Add(GetHandCard(owner, handSlot));
        owner.Hand.RemoveAt(handSlot);
        owner.ReadyShards++;
        owner.FedWell = true;
    }

    /// <summary>Spends the second player's once-per-duel Spark during Main.</summary>
    public void UseSpark(int player)
    {
        var owner = RequirePhase(player, DuelPhase.Main);
        Require(owner.HasSpark, "That player has no Spark left.");
        owner.HasSpark = false;
        owner.TemporaryEnergy++;
    }

    /// <summary>Lists available placements and targets without changing the duel.</summary>
    public IReadOnlyList<CardPlay> LegalPlays(int player, int handSlot)
    {
        var owner = GetPlayer(player);
        if (Phase != DuelPhase.Main || player != ActivePlayer || (uint)handSlot >= owner.Hand.Count)
        {
            return [];
        }

        var card = owner.Hand[handSlot];
        if (card.Cost > owner.ReadyShards + owner.TemporaryEnergy)
        {
            return [];
        }

        var slots = card.Type switch
        {
            CardType.Unit => EmptySlots(owner.Units),
            CardType.Relic => EmptySlots(owner.Relics),
            _ => new int?[] { null },
        };
        var abilities = PlayAbilities(card);
        if (IsTargetLegal(player, abilities, null))
        {
            return slots.Select(slot => new CardPlay(slot, null)).ToArray();
        }

        var plays = new List<CardPlay>();
        for (var side = 0; side < players.Length; side++)
        {
            for (var unit = 0; unit < UnitSlots; unit++)
            {
                var target = new UnitTarget(side, unit);
                if (IsTargetLegal(player, abilities, target))
                {
                    plays.AddRange(slots.Select(slot => new CardPlay(slot, target)));
                }
            }
        }
        return plays;
    }

    private static int?[] EmptySlots<T>(T?[] row) where T : class =>
        Enumerable.Range(0, row.Length).Where(slot => row[slot] is null).Select(slot => (int?)slot).ToArray();

    private static CardAbility[] PlayAbilities(CardDefinition card)
    {
        var trigger = card.Type == CardType.Spell ? AbilityTrigger.Spell : AbilityTrigger.Arrival;
        return card.Abilities.Where(ability => ability.Trigger == trigger).ToArray();
    }

    /// <summary>Pays for a card and resolves its Main-phase play.</summary>
    public void Play(int player, int handSlot, UnitTarget? target = null, int? boardSlot = null)
    {
        var owner = RequirePhase(player, DuelPhase.Main);
        var card = GetHandCard(owner, handSlot);
        var slot = card.Type switch
        {
            CardType.Unit => ChooseSlot(owner.Units, boardSlot),
            CardType.Relic => ChooseSlot(owner.Relics, boardSlot),
            _ when boardSlot is null => 0,
            _ => throw new InvalidOperationException("That card does not occupy a board slot."),
        };
        var abilities = PlayAbilities(card);
        ValidateTargets(player, abilities, target);
        Pay(owner, card.Cost);
        owner.Hand.RemoveAt(handSlot);
        switch (card.Type)
        {
            case CardType.Unit:
                owner.Units[slot] = new Unit(card, Turn);
                break;
            case CardType.Relic:
                owner.Relics[slot] = card;
                break;
            case CardType.Spell:
                owner.Discard.Add(card);
                break;
        }
        ResolveAbilities(player, abilities, target);
        Cleanup();
    }

    private static int ChooseSlot<T>(T?[] row, int? requested) where T : class
    {
        var slot = requested ?? Array.IndexOf(row, null);
        Require((uint)slot < row.Length && row[slot] is null, "Choose an open slot for that card.");
        return slot;
    }

    /// <summary>Uses an activated ability on one of the active player's Units or Relics.</summary>
    public void Activate(int player, bool relic, int slot, int abilityIndex, UnitTarget? target = null)
    {
        var owner = RequirePhase(player, DuelPhase.Main);
        var card = relic ? (uint)slot < RelicSlots ? owner.Relics[slot] : null
            : GetUnit(new(player, slot))?.Card;
        Require(card is not null, "There is no card in that slot.");
        var abilities = card!.Abilities;
        Require((uint)abilityIndex < abilities.Count && abilities[abilityIndex].Trigger == AbilityTrigger.Activated,
            "That ability is not activated.");
        var ability = abilities[abilityIndex];
        ValidateTargets(player, [ability], target);
        Pay(owner, ability.Cost);
        ResolveAbilities(player, [ability], target);
        Cleanup();
    }

    /// <summary>Declares an attack against a Unit, or against the enemy Leader if the target is null.</summary>
    public void Attack(int player, int attackerSlot, UnitTarget? target = null)
    {
        RequirePhase(player, DuelPhase.Battle);
        Require(pendingAttack is null, "Resolve the previous attack first.");
        var attackerAddress = new UnitTarget(player, attackerSlot);
        var attacker = GetUnit(attackerAddress);
        Require(attacker is not null && CanAttack(attacker), "That Unit cannot attack.");
        Require(target is null || (target.Value.Player == 1 - player && GetUnit(target.Value) is not null),
            "Choose the enemy Leader or a Unit on the enemy side.");
        attacker!.LastAttackTurn = Turn;
        if (!Has(attacker, CardKeyword.Vigilant))
        {
            attacker.Exhausted = true;
        }
        pendingAttack = new(attackerAddress, target);
    }

    /// <summary>Accepts the attack or redirects it to an eligible defending Unit, then resolves combat.</summary>
    public void Defend(int player, int? interceptSlot = null)
    {
        var defender = GetPlayer(player);
        Require(player == 1 - ActivePlayer && pendingAttack is not null, "There is no attack for this player to defend.");
        var attack = pendingAttack!;
        var attacker = GetUnit(attack.Attacker)!;
        var target = attack.Target;
        if (interceptSlot is int slot)
        {
            var address = new UnitTarget(player, slot);
            var interceptor = GetUnit(address);
            Require(interceptor is not null && CanIntercept(interceptor, attacker),
                "That Unit cannot intercept this attack.");
            interceptor!.Exhausted = true;
            interceptor.LastInterceptTurn = Turn;
            target = address;
        }

        pendingAttack = null;
        if (target is UnitTarget addressOfTarget)
        {
            var unit = GetUnit(addressOfTarget)!;
            var remaining = unit.Card.Health - unit.Damage;
            var dealtToUnit = Math.Min(attacker.Power, remaining);
            var attackerRemaining = attacker.Card.Health - attacker.Damage;
            var dealtBack = Math.Min(unit.Power, attackerRemaining);
            unit.Damage += attacker.Power;
            attacker.Damage += unit.Power;
            var dealtToLeader = Hurt(defender, PierceDamage(attacker, remaining));
            var dealtBackToLeader = Hurt(players[ActivePlayer], PierceDamage(unit, attackerRemaining));
            Drain(ActivePlayer, attacker, dealtToUnit + dealtToLeader);
            Drain(player, unit, dealtBack + dealtBackToLeader);
        }
        else
        {
            Drain(ActivePlayer, attacker, Hurt(defender, attacker.Power));
        }
        Cleanup();
    }

    private bool CanAttack(Unit unit) => !unit.Exhausted && unit.LastAttackTurn != Turn &&
        (unit.ArrivalTurn < Turn || Has(unit, CardKeyword.Rush));

    private bool CanIntercept(Unit unit, Unit attacker) => unit.LastInterceptTurn != Turn &&
        (!unit.Exhausted || Has(unit, CardKeyword.Sentinel)) &&
        (!Has(attacker, CardKeyword.Elusive) || Has(unit, CardKeyword.Elusive));

    private static int PierceDamage(Unit unit, int opposingHealth) =>
        Has(unit, CardKeyword.Pierce) ? Math.Max(0, unit.Power - opposingHealth) : 0;

    private static bool Has(Unit unit, CardKeyword keyword) => (unit.Card.Keywords & keyword) != 0;

    private void Drain(int player, Unit source, int damage)
    {
        if (Has(source, CardKeyword.Drain))
        {
            Heal(players[player], damage);
        }
    }

    private static int Hurt(Player player, int damage)
    {
        var dealt = Math.Min(player.Health, damage);
        player.Health -= dealt;
        return dealt;
    }

    private static void Heal(Player player, int amount)
    {
        if (player.Health > 0)
        {
            player.Health = Math.Min(StartingHealth, player.Health + amount);
        }
    }

    private void ResolveBoardTriggers(int player, AbilityTrigger trigger)
    {
        var owner = players[player];
        var cards = owner.Units.OfType<Unit>().Select(unit => unit.Card)
            .Concat(owner.Relics.OfType<CardDefinition>());
        var abilities = cards.SelectMany(card => card.Abilities).Where(ability => ability.Trigger == trigger).ToArray();
        ResolveAbilities(player, abilities, null);
        Cleanup();
    }

    private void ResolveAbilities(int player, IReadOnlyList<CardAbility> abilities, UnitTarget? target)
    {
        foreach (var ability in abilities)
        {
            var owner = players[player];
            var unit = ability.Target switch
            {
                AbilityTarget.ChosenUnit or AbilityTarget.FriendlyUnit when target is UnitTarget address => GetUnit(address),
                AbilityTarget.RandomFriendlyUnit => RandomUnit(owner),
                _ => null,
            };
            switch (ability.Action)
            {
                case AbilityAction.HealLeader:
                    Heal(owner, ability.Amount);
                    break;
                case AbilityAction.Draw:
                    Draw(owner, ability.Amount);
                    break;
                case AbilityAction.Damage when unit is not null:
                    unit.Damage += ability.Amount;
                    break;
                case AbilityAction.Ready when unit is not null:
                    unit.Exhausted = false;
                    break;
                case AbilityAction.IncreasePower when unit is not null:
                    unit.Power += ability.Amount;
                    break;
            }
        }
    }

    private Unit? RandomUnit(Player owner)
    {
        var units = owner.Units.OfType<Unit>().ToArray();
        return units.Length == 0 ? null : units[random.Next(units.Length)];
    }

    private void ValidateTargets(int player, IReadOnlyList<CardAbility> abilities, UnitTarget? target) =>
        Require(IsTargetLegal(player, abilities, target), "Choose a valid target for this effect.");

    private bool IsTargetLegal(int player, IReadOnlyList<CardAbility> abilities, UnitTarget? target)
    {
        var friendly = abilities.Any(ability => ability.Target == AbilityTarget.FriendlyUnit);
        if (!friendly && !abilities.Any(ability => ability.Target == AbilityTarget.ChosenUnit))
        {
            return target is null;
        }

        return target is UnitTarget address && (uint)address.Player < players.Length &&
            (!friendly || address.Player == player) && GetUnit(address) is not null;
    }

    private void Cleanup()
    {
        // Remove every lethal Unit before resolving any Last Breath from that group.
        while (true)
        {
            var lastBreaths = new List<(int Player, CardAbility Ability)>();
            var destroyed = false;
            for (var side = 0; side < players.Length; side++)
            {
                var player = (ActivePlayer + side) % players.Length;
                var owner = players[player];
                for (var slot = 0; slot < owner.Units.Length; slot++)
                {
                    var unit = owner.Units[slot];
                    if (unit is null || unit.Damage < unit.Card.Health)
                    {
                        continue;
                    }
                    destroyed = true;
                    owner.Units[slot] = null;
                    owner.Discard.Add(unit.Card);
                    lastBreaths.AddRange(unit.Card.Abilities.Where(ability => ability.Trigger == AbilityTrigger.LastBreath)
                        .Select(ability => (player, ability)));
                }
            }
            if (!destroyed)
            {
                break;
            }
            foreach (var (player, ability) in lastBreaths)
            {
                ResolveAbilities(player, [ability], null);
            }
        }
        CheckOutcome();
    }

    private static void Draw(Player owner, int count)
    {
        for (var i = 0; i < count; i++)
        {
            if (owner.Deck.Count == 0)
            {
                owner.DeckOut = true;
                return;
            }
            owner.Hand.Add(owner.Deck[^1]);
            owner.Deck.RemoveAt(owner.Deck.Count - 1);
        }
    }

    private void SetNextPhase(DuelPhase next)
    {
        CheckOutcome();
        if (Outcome == DuelOutcome.Ongoing)
        {
            Phase = next;
        }
    }

    private void CheckOutcome()
    {
        var firstLost = players[0].Health == 0 || players[0].DeckOut;
        var secondLost = players[1].Health == 0 || players[1].DeckOut;
        Outcome = (firstLost, secondLost) switch
        {
            (true, true) => DuelOutcome.Draw,
            (true, false) => DuelOutcome.PlayerTwoWins,
            (false, true) => DuelOutcome.PlayerOneWins,
            _ => DuelOutcome.Ongoing,
        };
        if (Outcome != DuelOutcome.Ongoing)
        {
            Phase = DuelPhase.Finished;
        }
    }

    private static void Pay(Player owner, int cost)
    {
        Require(owner.ReadyShards + owner.TemporaryEnergy >= cost, "Insufficient energy.");
        var temporarySpent = Math.Min(cost, owner.TemporaryEnergy);
        owner.TemporaryEnergy -= temporarySpent;
        owner.ReadyShards -= cost - temporarySpent;
    }

    private Player RequireActive(int player)
    {
        var owner = GetPlayer(player);
        Require(Outcome == DuelOutcome.Ongoing && player == ActivePlayer, "It is not that player's turn.");
        return owner;
    }

    private Player RequirePhase(int player, DuelPhase phase)
    {
        var owner = RequireActive(player);
        Require(Phase == phase, $"This action requires the {phase} phase.");
        return owner;
    }

    private Player GetPlayer(int player) => (uint)player < players.Length ? players[player]
        : throw new ArgumentOutOfRangeException(nameof(player));

    private Unit? GetUnit(UnitTarget address) => (uint)address.Slot < UnitSlots
        ? GetPlayer(address.Player).Units[address.Slot]
        : null;

    private static CardDefinition GetHandCard(Player player, int slot) => (uint)slot < player.Hand.Count
        ? player.Hand[slot]
        : throw new ArgumentOutOfRangeException(nameof(slot));

    private static int[] CheckedHandSlots(Player owner, IEnumerable<int> handSlots)
    {
        var slots = handSlots.ToArray();
        if (slots.Distinct().Count() != slots.Length || slots.Any(slot => (uint)slot >= owner.Hand.Count))
        {
            throw new ArgumentException("Choose distinct slots from the opening hand.", nameof(handSlots));
        }
        return slots;
    }

    private static List<CardDefinition> ValidateDeck(IEnumerable<CardDefinition> deck)
    {
        var cards = deck.ToList();
        if (cards.Count != DeckSize ||
            cards.Any(card => card.Type is not (CardType.Unit or CardType.Spell or CardType.Relic)
                || card.Cost is < 0 or > WellLimit || (card.Type == CardType.Unit && (card.Power < 0 || card.Health <= 0))
                || card.Abilities.Any(ability => ability.Amount < 0 || ability.Cost is < 0 or > WellLimit)) ||
            cards.GroupBy(card => card.Name, StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 3))
        {
            throw new ArgumentException("A deck needs 40 valid playable cards, with at most three of each name.", nameof(deck));
        }
        return cards.Select(card => card with { Abilities = Array.AsReadOnly(card.Abilities.ToArray()) }).ToList();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private sealed class Player(CardDefinition leader, List<CardDefinition> deck)
    {
        public CardDefinition Leader { get; } = leader;
        public List<CardDefinition> Deck { get; } = deck;
        public List<CardDefinition> Hand { get; } = [];
        public List<CardDefinition> Discard { get; } = [];
        public List<CardDefinition> Well { get; } = [];
        public Unit?[] Units { get; } = new Unit?[UnitSlots];
        public CardDefinition?[] Relics { get; } = new CardDefinition?[RelicSlots];
        public int Health { get; set; } = StartingHealth;
        public int ReadyShards { get; set; }
        public int TemporaryEnergy { get; set; }
        public bool HasSpark { get; set; }
        public bool FedWell { get; set; }
        public bool MulliganComplete { get; set; }
        public bool DeckOut { get; set; }
    }

    private sealed class Unit(CardDefinition card, int arrivalTurn)
    {
        public CardDefinition Card { get; } = card;
        public int ArrivalTurn { get; } = arrivalTurn;
        public int Power { get; set; } = card.Power;
        public int Damage { get; set; }
        public bool Exhausted { get; set; }
        public int LastAttackTurn { get; set; }
        public int LastInterceptTurn { get; set; }
    }
}
