using System.Globalization;
using EchoesOfTheShardveil.Core;

namespace EchoesOfTheShardveil.Desktop;

/// <summary>Provides a local hot-seat playtest without initializing the graphics device.</summary>
internal static class AlphaDuelConsole
{
    private const int PlayerCount = 2;

    /// <summary>Plays one duel in the console until it ends or the input is closed.</summary>
    public static void Run()
    {
        var duel = new Duel(StarterDecks.Create(), StarterDecks.Create(secondDeck: true));
        Console.WriteLine("Echoes of the Shardveil • Alpha.02 hot-seat duel");
        if (!ChooseOpeningHands(duel) || !PlayTurns(duel))
        {
            return;
        }

        Console.WriteLine(duel.Outcome switch
        {
            DuelOutcome.PlayerOneWins => "Player 1 wins.",
            DuelOutcome.PlayerTwoWins => "Player 2 wins.",
            _ => "The duel is a draw.",
        });
    }

    /// <summary>Asks each player in turn which cards to exchange.</summary>
    /// <param name="duel">A duel in setup.</param>
    /// <returns><see langword="false"/> if the input ended.</returns>
    private static bool ChooseOpeningHands(Duel duel)
    {
        for (var player = 0; player < PlayerCount; player++)
        {
            if (!PassTo(player))
            {
                return false;
            }

            while (true)
            {
                ShowDuel(duel, player);
                Console.Write("Mulligan: enter hand numbers to exchange, or Enter to keep > ");
                var input = Console.ReadLine();
                if (input is null)
                {
                    return false;
                }

                try
                {
                    duel.Mulligan(player, SplitWords(input).Select(ParseIndex));
                    break;
                }
                catch (Exception error) when (error is ArgumentException or FormatException or OverflowException)
                {
                    Console.WriteLine(error.Message);
                }
            }
        }
        return true;
    }

    /// <summary>Reads commands until the duel is over.</summary>
    /// <param name="duel">A duel past setup.</param>
    /// <returns><see langword="false"/> if a player quit or the input ended first.</returns>
    private static bool PlayTurns(Duel duel)
    {
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
                return false;
            }
            previousViewer = viewer;
            ShowDuel(duel, viewer);
            Console.Write("> ");
            var input = Console.ReadLine();
            if (input is null || input.Equals("quit", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            try
            {
                Execute(duel, viewer, SplitWords(input.ToLowerInvariant()));
            }
            catch (Exception error) when (error is ArgumentException or InvalidOperationException
                or FormatException or OverflowException)
            {
                Console.WriteLine(error.Message);
            }
        }
        return true;
    }

    /// <summary>Performs one command, or prints help for an unknown one.</summary>
    /// <param name="duel">The duel.</param>
    /// <param name="player">The index of the player typing.</param>
    /// <param name="words">The command split into words.</param>
    private static void Execute(Duel duel, int player, string[] words)
    {
        switch (words)
        {
            case ["next"]:
                duel.Advance(player);
                break;
            case ["well", var hand]:
                duel.FeedWell(player, ParseIndex(hand));
                break;
            case ["spark"]:
                duel.UseSpark(player);
                break;
            case ["play", var hand]:
                duel.Play(player, ParseIndex(hand));
                break;
            case ["play", var hand, "own", var unit]:
                duel.Play(player, ParseIndex(hand), new(player, ParseIndex(unit)));
                break;
            case ["play", var hand, "enemy", var unit]:
                duel.Play(player, ParseIndex(hand), new(1 - player, ParseIndex(unit)));
                break;
            case ["attack", var unit]:
                duel.Attack(player, ParseIndex(unit));
                break;
            case ["attack", var unit, var target]:
                duel.Attack(player, ParseIndex(unit), new(1 - player, ParseIndex(target)));
                break;
            case ["take"]:
                duel.Defend(player);
                break;
            case ["intercept", var unit]:
                duel.Defend(player, ParseIndex(unit));
                break;
            case ["discard", var hand]:
                duel.DiscardToLimit(player, ParseIndex(hand));
                break;
            default:
                Console.WriteLine("Main: well HAND, spark, play HAND [own|enemy UNIT], next");
                Console.WriteLine("Battle: attack UNIT [ENEMY-UNIT], next. Omit the target to attack the Leader.");
                Console.WriteLine("Defend: take, intercept UNIT. End: discard HAND, next. Exit: quit.");
                break;
        }
    }

    /// <summary>Prints the table as one player may see it.</summary>
    /// <param name="duel">The duel.</param>
    /// <param name="viewer">The index of the player looking.</param>
    private static void ShowDuel(Duel duel, int viewer)
    {
        Console.WriteLine($"\nTurn {duel.Turn} · Player {duel.ActivePlayer + 1} · {duel.Phase}");
        for (var side = 0; side < PlayerCount; side++)
        {
            ShowSide(side, duel.View(side, viewer));
        }

        if (duel.PendingAttack is { } attack)
        {
            var target = attack.Target is { } unit ? $"Unit {unit.Slot + 1}" : "your Leader";
            Console.WriteLine($"Unit {attack.Attacker.Slot + 1} attacks {target}. Enter take or intercept UNIT.");
        }
    }

    /// <summary>Prints one side's counts, board and, if visible, hand.</summary>
    /// <param name="side">The player index.</param>
    /// <param name="view">The side as the viewer may see it.</param>
    private static void ShowSide(int side, PlayerView view)
    {
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

    /// <summary>Hides the screen while the keyboard changes hands.</summary>
    /// <param name="player">The index of the next player.</param>
    /// <returns><see langword="false"/> if the input ended.</returns>
    private static bool PassTo(int player)
    {
        if (!Console.IsOutputRedirected)
        {
            Console.Clear();
        }
        Console.WriteLine($"Pass control to Player {player + 1}, then press Enter.");
        return Console.ReadLine() is not null;
    }

    /// <summary>Splits a line on white space.</summary>
    /// <param name="input">The line.</param>
    /// <returns>The words.</returns>
    private static string[] SplitWords(string input) =>
        input.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

    /// <summary>Converts the one-based number a player types to a zero-based index.</summary>
    /// <param name="input">The typed number.</param>
    /// <returns>The index.</returns>
    private static int ParseIndex(string input) => checked(int.Parse(input, CultureInfo.InvariantCulture) - 1);
}
