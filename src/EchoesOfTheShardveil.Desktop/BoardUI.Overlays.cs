using EchoesOfTheShardveil.Core;
using Microsoft.Xna.Framework;

namespace EchoesOfTheShardveil.Desktop;

// Draws the panels that cover the table: the tutorial, the card inspector and the tooltips.
internal sealed partial class BoardUI
{
    private const int InspectorVisibleRows = 6;

    private static readonly Color OverlayFill = new(19, 34, 43);
    private static readonly Color OverlayEdge = new(73, 110, 120);

    private static readonly Tooltip HealthTooltip =
        new("HEALTH", "Your Hero's remaining Health. Units and effects reduce it, and you lose at 0.");
    private static readonly Tooltip WellTooltip =
        new("WELL", "Shards in your Well. Each one refills at the start of your turn and pays 1 energy. Once per turn, drag a card onto your portrait to add a shard.");
    private static readonly Tooltip OpponentHealthTooltip =
        new("OPPONENT", "Their remaining Health. Attack this portrait to reduce it.");
    private static readonly Tooltip OpponentWellTooltip =
        new("WELL", "Shards in the Opponent's Well. Each ready shard pays for 1 energy.");
    private static readonly Tooltip DiscardTooltip =
        new("DISCARD", "Cards in your discard pile: used Spells, destroyed Units, and cards discarded to the hand limit.");
    private static readonly Tooltip DeckTooltip =
        new("DECK", "Cards left in your deck. You draw one at the start of each turn, and you lose if you must draw from an empty deck.");
    private static readonly Tooltip RemovedTooltip =
        new("REMOVED", "Cards removed from the game. They can't return. No cards use this yet.");

    private static readonly CardDefinition TutorialUnit =
        StarterDecks.Create().First(card => card.Id == "mistwing-scout");

    /// <summary>Draws help for the badge or counter under the pointer.</summary>
    private void DrawBadgeTooltip()
    {
        if (handDrag is { Started: true } || hoverPointer is not { } pointer)
        {
            return;
        }

        var player = pointer.Y < layoutHeight / 2 ? Opponent : Human;
        var view = player == Human ? humanView : opponentView;
        var portrait = PortraitBounds(player);
        var counters = CardCounters(portrait, view);
        var hovered = PortraitBadges(player, view).Where(badge => Geometry.IsInCircle(badge.Bounds, pointer))
            .Concat(counters.Where(counter => counter.Bounds.Contains(pointer))).ToArray();
        if (hovered.Length == 0)
        {
            return;
        }

        var (badge, _, (heading, body)) = hovered[0];
        var deck = counters[2].Bounds;

        var lines = BitmapText.Wrap(body, 34);
        const int padding = 12;
        const int bodySize = 16;
        var width = 2 * padding + BitmapText.Advance(bodySize) * Math.Max(heading.Length, lines.Max(line => line.Length));
        var height = 2 * padding + 20 + lines.Length * (bodySize + 4);
        // Show it beside the portrait, level with the badge, so it never covers the portrait.
        var y = Math.Clamp(badge.Center.Y - height / 2, 8, layoutHeight - height - 8);
        var bounds = new Rectangle(Math.Max(badge.Right, portrait.Right) + 10, y, width, height);
        if (bounds.Intersects(deck) && badge != deck)
        {
            // Keep the tooltip from covering the Deck counter.
            bounds.X = deck.Right + 10;
        }
        DrawBox(bounds, OverlayFill, OverlayEdge);
        DrawText(heading, bounds.X + padding, bounds.Y + padding, 15, Teal);
        for (var i = 0; i < lines.Length; i++)
        {
            DrawText(lines[i], bounds.X + padding, bounds.Y + padding + 20 + i * (bodySize + 4), bodySize, Ivory);
        }
    }

    /// <summary>Draws the tutorial prompt or the current lesson over a dimmed table.</summary>
    private void DrawTutorial()
    {
        FillRectangle(FullWidth(0, layoutHeight), new Color(0, 0, 0) * 0.75f);
        DrawBox(CenterInWindow(TutorialPanel), OverlayFill, OverlayEdge);
        var left = CenterInWindow(TutorialPanel).X + 36;
        if (tutorial.IsPrompt)
        {
            DrawText("BEFORE THE DUEL", left, 161 + TutorialOffset, 21, Teal);
            DrawText("Want to learn the rules?", left, 207 + TutorialOffset, 43, Ivory);
            DrawText("Learn the board layout, card details, and rules of a duel.", left, 280 + TutorialOffset, 23, Muted);
            DrawText("You can skip the guide and go straight to your opening hand.", left, 325 + TutorialOffset, 21, Muted);
            DrawButton(CenterInWindow(TutorialSkip), "Skip for now", false, quiet: true);
            DrawButton(CenterInWindow(TutorialNext), "Learn the rules", true);
            return;
        }

        var lesson = BoardTutorial.Pages[tutorial.PageIndex];
        DrawText($"HOW TO PLAY  /  {tutorial.PageIndex + 1} OF {BoardTutorial.Pages.Count}", left,
            151 + TutorialOffset, 19, Teal);
        DrawText(lesson.Title, left, 191 + TutorialOffset, 37, Ivory);
        var pointsX = left;
        var lineWidth = 64;
        if (lesson.Illustration != TutorialIllustration.None)
        {
            // Make room for the picture on the left.
            pointsX += 186;
            lineWidth = 48;
        }
        switch (lesson.Illustration)
        {
            case TutorialIllustration.Board:
                DrawTutorialBoard(left, 254 + TutorialOffset);
                break;
            case TutorialIllustration.Card:
                var example = CardBounds(left + 12, 245 + TutorialOffset, 135);
                DrawCard(example, TutorialUnit, false, new(TutorialUnit, TutorialUnit.Power, 1, false, true, false));
                DrawCenteredText("EXAMPLE UNIT", example.Center.X, example.Bottom + 8, 14, Muted);
                break;
        }
        DrawTutorialPoints(lesson.Points, pointsX, lineWidth);

        var progressY = 503 + TutorialOffset;
        for (var i = 0; i < BoardTutorial.Pages.Count; i++)
        {
            FillRectangle(new(left + i * 30, progressY, 23, 3), i <= tutorial.PageIndex ? Teal : Border);
        }
        DrawButton(CenterInWindow(TutorialBack), "Back", false, quiet: true);
        DrawButton(CenterInWindow(TutorialSkip), "Skip guide", false, quiet: true);
        DrawButton(CenterInWindow(TutorialNext), tutorial.IsLastPage ? "Start duel" : "Next", true);
    }

    /// <summary>Draws a lesson's bulleted paragraphs.</summary>
    /// <param name="points">The paragraphs.</param>
    /// <param name="left">The left edge.</param>
    /// <param name="lineWidth">The most characters on a line.</param>
    private void DrawTutorialPoints(IReadOnlyList<string> points, int left, int lineWidth)
    {
        var y = 250 + TutorialOffset;
        foreach (var point in points)
        {
            FillRectangle(new(left + 1, y + 8, 5, 5), Teal);
            foreach (var line in BitmapText.Wrap(point, lineWidth, 5))
            {
                DrawText(line, left + 18, y, 21, Ivory);
                y += 23;
            }
            y += 10;
        }
    }

    /// <summary>Draws a miniature of the board: each side's portrait and slots, then the actions and hand.</summary>
    /// <param name="left">The left edge.</param>
    /// <param name="top">The top edge.</param>
    private void DrawTutorialBoard(int left, int top)
    {
        const int width = 160;
        DrawBox(new(left, top, width, 199), Recess, Border);
        DrawCenteredText("EXIT / SOUND", left + width / 2, top + 9, 14, Muted);
        for (var side = 0; side < 2; side++)
        {
            var y = top + 36 + side * 77;
            var accent = side == 0 ? Rose : Teal;
            DrawText(side == 0 ? "OPPONENT" : "YOU", left + 11, y, 14, accent);
            FillRectangle(new(left + 12, y + 19, 19, 24), accent);
            for (var slot = 0; slot < Duel.UnitSlots + Duel.RelicSlots; slot++)
            {
                var x = left + 40 + slot * 15 + (slot >= Duel.UnitSlots ? 3 : 0);
                FillRectangle(new(x, y + 23, 11, 16), slot < Duel.UnitSlots ? Border : Gold * 0.65f);
            }
        }
        DrawCenteredText("ACTIONS", left + width / 2, top + 159, 14, Muted);
        DrawCenteredText("YOUR HAND", left + width / 2, top + 177, 14, Teal);
    }

    /// <summary>Draws the details of a card.</summary>
    /// <param name="card">The card being inspected.</param>
    private void DrawInspector(CardDefinition card)
    {
        DrawBox(AnchorRight(InspectorPanel), OverlayFill, OverlayEdge);
        DrawText("CARD DETAILS", 631 + ExtraWidth, 94, 15, Teal);
        DrawButton(AnchorRight(InspectorClose), "Close", false, quiet: true);
        var preview = CardBounds(628 + ExtraWidth, 121, 140);
        DrawCard(preview, card, true, enlarged: true);

        var left = 789 + ExtraWidth;
        DrawText(card.Name, left, 107, 30, Ivory);
        var type = card.Subtype is { Length: > 0 } subtype ? $"{card.Type} / {subtype}" : card.Type.ToString();
        DrawText($"{type}   |   {card.Cost} energy", left, 144, 18, Teal);
        var stats = card.Type == CardType.Unit ? $"{card.Power} Power / {card.Health} Health" : "";
        if (card.Aspect is { Length: > 0 } aspect)
        {
            stats = stats.Length > 0 ? $"{stats}   |   Aspect: {aspect}" : $"Aspect: {aspect}";
        }
        DrawText(stats, left, 166, 17, Muted);

        if (inspectorRows.Length == 0)
        {
            DrawText("No keyword or ability text.", left, 205, 18, Muted);
        }
        for (var i = 0; i < InspectorVisibleRows && i + inspectorScroll < inspectorRows.Length; i++)
        {
            DrawText(inspectorRows[i + inspectorScroll], left, 190 + i * 20, 18, Ivory);
        }
        if (inspectorRows.Length > InspectorVisibleRows)
        {
            DrawText("Scroll for more", left, 313, 14, Muted);
        }
    }

    /// <summary>The help shown when hovering over a badge or counter.</summary>
    /// <param name="Heading">The short title.</param>
    /// <param name="Body">The explanation.</param>
    private sealed record Tooltip(string Heading, string Body);
}
