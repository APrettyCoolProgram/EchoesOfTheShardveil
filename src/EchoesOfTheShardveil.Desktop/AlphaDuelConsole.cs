using EchoesOfTheShardveil.Core;

namespace EchoesOfTheShardveil.Desktop;

/// <summary>Provides a local hot-seat playtest without initializing the graphics device.</summary>
internal static class AlphaDuelConsole
{
    public static void Run()
    {
        var duel = new Duel(StarterDecks.Create(), StarterDecks.Create(secondDeck: true));
        Console.WriteLine("Echoes of the Shardveil • Alpha.02 hot-seat duel");
        for (var player = 0; player < 2; player++)
        {
            if (!PassTo(player))
            {
                return;
            }

            while (true)
            {
                Show(duel, player);
                Console.Write("Mulligan: enter hand numbers to exchange, or Enter to keep > ");
                var input = Console.ReadLine();
                if (input is null)
                {
                    return;
                }

                try
                {
                    duel.Mulligan(player, Split(input).Select(Index));
                    break;
                }
                catch (ArgumentException error)
                {
                    Console.WriteLine(error.Message);
                }
                catch (FormatException error)
                {
                    Console.WriteLine(error.Message);
                }
                catch (OverflowException error)
                {
                    Console.WriteLine(error.Message);
                }
            }
        }

        int? previousViewer = null;
        while (duel.Outcome == DuelOutcome.Ongoing)
        {
            var active = duel.ActivePlayer;
            if (duel.Phase is DuelPhase.Ready or DuelPhase.Draw)
            {
                duel.Advance(active);
                continue;
            }

            var viewer = duel.PendingAttack is null ? active : 1 - active;
            if (viewer != previousViewer && !PassTo(viewer))
            {
                return;
            }
            previousViewer = viewer;
            Show(duel, viewer);
            Console.Write("> ");
            var input = Console.ReadLine();
            if (input is null || input.Equals("quit", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            try
            {
                Execute(duel, viewer, Split(input.ToLowerInvariant()));
            }
            catch (Exception error) when (error is ArgumentException or InvalidOperationException
                or FormatException or OverflowException)
            {
                Console.WriteLine(error.Message);
            }
        }

        Console.WriteLine(duel.Outcome switch
        {
            DuelOutcome.PlayerOneWins => "Player 1 wins.",
            DuelOutcome.PlayerTwoWins => "Player 2 wins.",
            _ => "The duel is a draw.",
        });
    }

    private static void Execute(Duel duel, int player, string[] words)
    {
        switch (words)
        {
            case ["next"]:
                duel.Advance(player);
                break;
            case ["well", var hand]:
                duel.FeedWell(player, Index(hand));
                break;
            case ["spark"]:
                duel.UseSpark(player);
                break;
            case ["play", var hand]:
                duel.Play(player, Index(hand));
                break;
            case ["play", var hand, "own", var unit]:
                duel.Play(player, Index(hand), new(player, Index(unit)));
                break;
            case ["play", var hand, "enemy", var unit]:
                duel.Play(player, Index(hand), new(1 - player, Index(unit)));
                break;
            case ["attack", var unit]:
                duel.Attack(player, Index(unit));
                break;
            case ["attack", var unit, var target]:
                duel.Attack(player, Index(unit), new(1 - player, Index(target)));
                break;
            case ["take"]:
                duel.Defend(player);
                break;
            case ["intercept", var unit]:
                duel.Defend(player, Index(unit));
                break;
            case ["discard", var hand]:
                duel.DiscardToLimit(player, Index(hand));
                break;
            default:
                Console.WriteLine("Main: well HAND, spark, play HAND [own|enemy UNIT], next");
                Console.WriteLine("Battle: attack UNIT [ENEMY-UNIT], next. Omit the target to attack the Leader.");
                Console.WriteLine("Defend: take, intercept UNIT. End: discard HAND, next. Exit: quit.");
                break;
        }
    }

    private static void Show(Duel duel, int viewer)
    {
        Console.WriteLine($"\nTurn {duel.Turn} · Player {duel.ActivePlayer + 1} · {duel.Phase}");
        for (var side = 0; side < 2; side++)
        {
            var view = duel.View(side, viewer);
            Console.WriteLine($"P{side + 1}: {view.Health} Health | {view.DeckCount} deck | {view.HandCount} hand" +
                $" | {view.ReadyShards + view.TemporaryEnergy} energy | {view.Shards} shards" +
                $" | {view.Discard.Count} discard" + (view.HasSpark ? " | Spark" : ""));
            var units = view.Units.Select((unit, index) => unit is null ? null :
                $"{index + 1}:{unit.Card.Name} {unit.Power}/{unit.Card.Health - unit.Damage}" +
                (unit.Exhausted ? " exhausted" : "") + (unit.CanAttack ? " [attack]" : "") +
                (unit.CanIntercept ? " [intercept]" : "")).OfType<string>();
            Console.WriteLine($"  Units: {string.Join(" | ", units)}");
            Console.WriteLine($"  Relics: {string.Join(", ", view.Relics.OfType<CardDefinition>().Select(card => card.Name))}");
            if (view.Hand is { } hand)
            {
                for (var i = 0; i < hand.Count; i++)
                {
                    var card = hand[i];
                    Console.WriteLine($"  {i + 1}. {card.Name} ({card.Cost}) — {card.Type}" +
                        (card.Type == CardType.Unit ? $" {card.Power}/{card.Health}" : "") +
                        (card.Keywords == CardKeyword.None ? "" : $" {card.Keywords}") +
                        string.Concat(card.Abilities.Select(ability =>
                            $" [{ability.Trigger}: {ability.Action} {ability.Amount}, {ability.Target}]")));
                }
            }
        }

        if (duel.PendingAttack is { } attack)
        {
            var target = attack.Target is { } unit ? $"Unit {unit.Slot + 1}" : "your Leader";
            Console.WriteLine($"Unit {attack.Attacker.Slot + 1} attacks {target}. Enter take or intercept UNIT.");
        }
    }

    private static bool PassTo(int player)
    {
        if (!Console.IsOutputRedirected)
        {
            Console.Clear();
        }
        Console.WriteLine($"Pass control to Player {player + 1}, then press Enter.");
        return Console.ReadLine() is not null;
    }

    private static string[] Split(string input) => input.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

    private static int Index(string input) =>
        checked(int.Parse(input, System.Globalization.CultureInfo.InvariantCulture) - 1);
}
