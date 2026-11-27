namespace EchoesOfTheShardveil.Desktop;

/// <summary>Tracks the optional pre-match rules walkthrough.</summary>
internal sealed class BoardTutorial
{
    /// <summary>Gets the lessons in the order they are shown.</summary>
    public static IReadOnlyList<TutorialPage> Pages { get; } =
    [
        new("Win the duel",
        [
            "Your Hero and the Opponent each start at 20 Health.",
            "Win by reducing the Opponent to zero or forcing them to draw from an empty deck. If both sides lose together, the duel is a draw.",
            "Your cards and controls are at the bottom. The opponent's side is above the center line.",
        ]),
        new("Find your way around",
        [
            "The Opponent's portrait and cards are at the top. Your in-play cards are on the lower half, and your Hero portrait is in the lower left.",
            "Brief updates about the duel appear below the action buttons. The sound icon toggles sound.",
            "Your hand is along the bottom. Hover to reveal a card or right-click for details. The exit icon offers to forfeit and return to the main menu.",
        ], TutorialIllustration.Board),
        new("Hero and counters",
        [
            "Your Health is centered at the bottom of the Hero portrait. The Opponent has the same Health badge on their portrait.",
            "The badge at the top of your portrait is your Well count. The card-shaped counters beside it show cards Removed from the game (upper left), your Discard pile (lower left) and your Deck (lower right). Hover any badge for a reminder. SPARK READY means Spark is available.",
            "The opposing hand and both decks stay hidden.",
        ]),
        new("Slots and discards",
        [
            "Each side has five Unit slots in the middle and two Relic slots on the right. A dashed outline is an empty slot.",
            "Units fight from their slots. Relics stay in play for repeatable or ongoing effects. A full row prevents more plays of its type.",
            "Used Spells, destroyed Units, and cards discarded to the hand limit go to that player's discard pile.",
        ]),
        new("Read a card",
        [
            "The top line shows type and energy cost. UNIT 2 means this Unit costs 2 energy.",
            "The name is directly beneath it. The rules area shows keywords, like Elusive, or a short ability summary.",
            "A Unit's bottom-left numbers are Power / Health. On the board, Health is the amount remaining after damage.",
        ], TutorialIllustration.Card),
        new("Read the marks",
        [
            "The stripe on an unselected card identifies its type: teal for Units, pink for Spells, and gold for Relics.",
            "A gold outline marks a selection or a possible target. Right-click a hand card to see its full rules.",
            "ATTACK and BLOCK mark Units that can act now. USED means exhausted. A Sentinel can still show BLOCK while exhausted.",
        ]),
        new("Choose an opening hand",
        [
            "Each deck has 40 cards, with up to three copies of a card by name. Each player draws five cards.",
            "Click any cards you want to replace, then confirm. They go to the bottom of your deck in random order before you draw replacements.",
            "A random player goes first and skips their first draw. The second player gets a Spark for one temporary energy.",
        ]),
        new("Follow the turn",
        [
            "Ready refills your Well and readies your exhausted Units. Then Draw gives you one card.",
            "In Main, feed the Well, use your Spark, and play cards. In Battle, attack one Unit at a time or skip combat.",
            "At End, discard to 10 cards if needed. Temporary energy expires and the other player takes their turn.",
        ]),
        new("Power your cards",
        [
            "Once during Main, drag any hand card onto your Hero portrait to feed the Well. It becomes a face-down shard and gives you one energy immediately.",
            "Each ready shard pays for one energy. Your shards refill on your next Ready phase. Unspent energy does not carry over.",
            "Use Spark once during your Main phase if you have it. The extra energy lasts for that turn only.",
        ]),
        new("Play a card",
        [
            "Drag a card to a highlighted slot. Its printed cost is paid from your energy. Drop targeted Spells on a highlighted Unit and other Spells in Cast Spell.",
            "Units occupy open Unit slots, Spells resolve and go to Discard, and Relics stay in open Relic slots for ongoing effects.",
            "Arrival resolves on entering play. Last Breath resolves when a Unit is destroyed. Some Relics act at the start of your turn.",
        ]),
        new("Use the action row",
        [
            "In Main, drag a card onto your portrait for energy or to a highlighted slot to play it. Use Spark adds 1 energy. Go to battle ends Main.",
            "During combat, click a Unit or the Opponent portrait to target it. Take the hit declines interception. End battle moves to End.",
            "Use Cancel selection to clear a card or target choice. At End, Discard if above the limit, then click End turn.",
        ]),
        new("Attack and intercept",
        [
            "Choose a Unit marked ATTACK, then click an opposing Unit or the Opponent portrait. Most Units cannot attack the turn they arrive.",
            "The defender can intercept with a Unit marked BLOCK or take the hit. Each attacker or interceptor acts at most once per turn.",
            "Units deal their Power simultaneously. The Hero and Opponent deal no damage back. A Unit shows Power / remaining Health and is destroyed at zero remaining Health.",
        ]),
        new("Read the keywords",
        [
            "Rush: Attack on arrival.  Vigilant: Stay ready after attacking.",
            "Sentinel: Intercept while exhausted.  Elusive: Only Elusive Units can intercept this attack.",
            "Pierce: Excess Unit combat damage hits the Opponent.  Drain: Restore your Hero by the damage dealt, up to 20 Health.",
        ]),
    ];

    /// <summary>Gets the current lesson, or -1 while asking whether to show the guide.</summary>
    public int PageIndex { get; private set; } = -1;

    /// <summary>Gets whether the walkthrough is still showing.</summary>
    public bool IsOpen { get; private set; } = true;

    /// <summary>Gets whether the opening question is showing.</summary>
    public bool IsPrompt => PageIndex < 0;

    /// <summary>Gets whether the final lesson is showing.</summary>
    public bool IsLastPage => PageIndex == Pages.Count - 1;

    /// <summary>Moves to the next lesson, or closes the walkthrough after the last one.</summary>
    public void Next()
    {
        if (IsLastPage)
        {
            Close();
        }
        else
        {
            PageIndex++;
        }
    }

    /// <summary>Moves to the previous lesson, or back to the opening question.</summary>
    public void Back() => PageIndex = Math.Max(-1, PageIndex - 1);

    /// <summary>Ends the walkthrough.</summary>
    public void Close() => IsOpen = false;
}

/// <summary>The picture shown beside a lesson.</summary>
internal enum TutorialIllustration
{
    /// <summary>Text only.</summary>
    None,
    /// <summary>A diagram of the board.</summary>
    Board,
    /// <summary>An example card.</summary>
    Card,
}

/// <summary>One lesson of the walkthrough.</summary>
/// <param name="Title">The heading.</param>
/// <param name="Points">The bulleted paragraphs.</param>
/// <param name="Illustration">The picture shown beside the text.</param>
internal sealed record TutorialPage(string Title, IReadOnlyList<string> Points,
    TutorialIllustration Illustration = TutorialIllustration.None);
