// 261127_code
// 261127_documentation

using System.Globalization;
using EchoesOfTheShardveil.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EchoesOfTheShardveil.Desktop;

// Draws the table: playmats, portraits and counters, board slots, the hand, cards and action buttons.
internal sealed partial class BoardUI
{
    // The badge atlas is a grid of numbered badges from 0 to the starting Health.
    private const int HealthAtlasColumns = 6;
    private const int HealthAtlasCount = Duel.StartingHealth + 1;

    /// <summary>The color behind the board.</summary>
    public static readonly Color BackgroundColor = new(12, 21, 27);

    private static readonly Color Ivory = UiColors.Ivory;
    private static readonly Color Muted = new(163, 176, 179);
    private static readonly Color Teal = new(101, 208, 189);
    private static readonly Color Gold = new(235, 184, 107);
    private static readonly Color Rose = new(233, 145, 146);
    private static readonly Color Panel = new(25, 39, 45);
    private static readonly Color Border = new(57, 82, 90);
    private static readonly Color Recess = new(14, 25, 31);

    /// <summary>Draws the whole board.</summary>
    /// <param name="view">The mapping for the current window.</param>
    /// <param name="rasterizer">A rasterizer state with scissoring enabled.</param>
    public void Draw(BoardViewport view, RasterizerState rasterizer)
    {
        SetLayout(view);
        spriteBatch.Begin(samplerState: SamplerState.LinearClamp, rasterizerState: rasterizer,
            transformMatrix: view.Transform);
        FillRectangle(FullWidth(0, layoutHeight), BackgroundColor);
        var split = layoutHeight / 2 - 22;
        DrawPlaymat(opponentPlaymat, FullWidth(0, split), verticalAnchor: 0.5f);
        DrawPlaymat(heroPlaymat, FullWidth(split, layoutHeight - split), verticalAnchor: 1f);
        DrawSide(Opponent, opponentView);
        DrawSide(Human, humanView);
        DrawStatus();
        DrawHand();
        DrawActions();
        DrawDrag();
        if (tutorial.IsOpen)
        {
            DrawTutorial();
        }
        else
        {
            if (inspectedCard is { } card)
            {
                DrawInspector(card);
            }
            DrawBadgeTooltip();
        }
        spriteBatch.End();
    }

    /// <summary>Draws a playmat, cropped to the shape of its area.</summary>
    /// <param name="texture">The artwork.</param>
    /// <param name="bounds">The area to fill.</param>
    /// <param name="verticalAnchor">
    /// Which part survives a vertical crop, from 0 for the top to 1 for the bottom.
    /// </param>
    private void DrawPlaymat(Texture2D texture, Rectangle bounds, float verticalAnchor)
    {
        var scale = Math.Max((float)bounds.Width / texture.Width, (float)bounds.Height / texture.Height);
        var width = Math.Clamp((int)MathF.Round(bounds.Width / scale), 1, texture.Width);
        var height = Math.Clamp((int)MathF.Round(bounds.Height / scale), 1, texture.Height);
        var source = new Rectangle((texture.Width - width) / 2,
            (int)MathF.Round((texture.Height - height) * verticalAnchor), width, height);
        spriteBatch.Draw(texture, bounds, source, Color.White);
    }

    /// <summary>Draws one player's portrait, counters and board slots.</summary>
    /// <param name="player">The player index.</param>
    /// <param name="view">The player's snapshot.</param>
    private void DrawSide(int player, PlayerView view)
    {
        var portrait = PortraitBounds(player);
        var attackTarget = player == Opponent && selectedAttacker is not null;
        spriteBatch.Draw(player == Human ? heroPortrait : opponentPortrait, portrait, Color.White);
        if (attackTarget || (player == Human && humanView.CanFeedWell && handDrag is { Started: true, Position: not null }))
        {
            var ring = portrait;
            ring.Inflate(3, 3);
            var hovered = handDrag?.Position is { } pointer && IsOverPortrait(player, pointer);
            spriteBatch.Draw(heroDropRing, ring, attackTarget || hovered ? Gold : Teal);
        }

        foreach (var (bounds, value, _) in PortraitBadges(player, view))
        {
            DrawBadge(bounds, value);
        }
        foreach (var (bounds, value, _) in CardCounters(portrait, view))
        {
            DrawCardCounter(bounds, value);
        }

        var statusY = player == Human ? SideTop(player) + 86 : portrait.Bottom + 24;
        if (view.HasSpark)
        {
            DrawText("SPARK READY", 28, statusY, 12, Gold);
        }
        if (view.TemporaryEnergy > 0)
        {
            DrawText($"+{view.TemporaryEnergy} ENERGY", 28, statusY + 18, 12, Gold);
        }

        for (var i = 0; i < Duel.UnitSlots; i++)
        {
            var bounds = UnitBounds(player, i);
            if (view.Units[i] is { } unit)
            {
                var isAttacker = player == Human && selectedAttacker == i;
                DrawCard(bounds, unit.Card, isAttacker || attackTarget, unit);
            }
            else
            {
                DrawEmptySlot(bounds, $"UNIT {i + 1}");
            }
        }

        for (var i = 0; i < Duel.RelicSlots; i++)
        {
            var bounds = RelicBounds(player, i);
            if (view.Relics[i] is { } relic)
            {
                DrawCard(bounds, relic, false);
            }
            else
            {
                DrawEmptySlot(bounds, $"RELIC {i + 1}");
            }
        }
    }

    /// <summary>Gets the Well and Health badges on a portrait.</summary>
    /// <param name="player">The player index.</param>
    /// <param name="view">The player's snapshot.</param>
    /// <returns>Each badge's position, number and help.</returns>
    private (Rectangle Bounds, int Value, Tooltip Tooltip)[] PortraitBadges(int player, PlayerView view)
    {
        var portrait = PortraitBounds(player);
        return
        [
            (RimBadge(portrait, 12), view.Shards, player == Human ? WellTooltip : OpponentWellTooltip),
            (RimBadge(portrait, 6), view.Health, player == Human ? HealthTooltip : OpponentHealthTooltip),
        ];
    }

    /// <summary>
    /// Gets the Removed, Discard and Deck counters, in that order. Removed is not in the rules yet and shows zero.
    /// </summary>
    /// <param name="portrait">The portrait the counters flank.</param>
    /// <param name="view">The player's snapshot.</param>
    /// <returns>Each counter's position, number and help.</returns>
    private static (Rectangle Bounds, int Value, Tooltip Tooltip)[] CardCounters(Rectangle portrait, PlayerView view)
    {
        var bottom = portrait.Bottom - CounterHeight;
        return
        [
            (new(CounterEdge, portrait.Top, CounterWidth, CounterHeight), 0, RemovedTooltip),
            (new(CounterEdge, bottom, CounterWidth, CounterHeight), view.Discard.Count, DiscardTooltip),
            (new(DeckCounterLeft, bottom, CounterWidth, CounterHeight), view.DeckCount, DeckTooltip),
        ];
    }

    /// <summary>Draws a card-shaped counter with a centered number.</summary>
    /// <param name="bounds">The counter bounds.</param>
    /// <param name="value">The number.</param>
    private void DrawCardCounter(Rectangle bounds, int value)
    {
        DrawBox(bounds, Color.Black, Color.White);
        const int gap = 1; // With odd digit widths this keeps the whole number's width odd.
        var label = Math.Max(0, value).ToString(CultureInfo.InvariantCulture);
        var digits = label.Select(character => counterDigits[character - '0']).ToArray();
        var total = digits.Sum(digit => digit.Width) + gap * (digits.Length - 1);
        var x = bounds.X + (bounds.Width - total) / 2;
        var y = bounds.Y + (bounds.Height - CounterDigitHeight) / 2;
        foreach (var digit in digits)
        {
            spriteBatch.Draw(digit, new Vector2(x, y), Color.White);
            x += digit.Width + gap;
        }
    }

    /// <summary>Draws a number from the round badge atlas shared by the portrait counters.</summary>
    /// <param name="bounds">The badge bounds.</param>
    /// <param name="value">The number to show.</param>
    private void DrawBadge(Rectangle bounds, int value)
    {
        if (value >= HealthAtlasCount)
        {
            // The atlas stops at 20, so larger numbers are drawn as text on a blank badge.
            spriteBatch.Draw(blankBadge, bounds, Color.White);
            var label = value.ToString(CultureInfo.InvariantCulture);
            DrawCenteredText(label, bounds.Center.X, bounds.Center.Y - 9, 18, Color.White);
            return;
        }

        var index = Math.Max(0, value);
        var cellSize = healthAtlas.Width / HealthAtlasColumns;
        var tile = new Rectangle(index % HealthAtlasColumns * cellSize, index / HealthAtlasColumns * cellSize,
            cellSize, cellSize);
        spriteBatch.Draw(healthAtlas, bounds, tile, Color.White);
    }

    /// <summary>Draws the result, the pending attack or the latest message.</summary>
    private void DrawStatus()
    {
        var status = duel.Outcome switch
        {
            DuelOutcome.PlayerOneWins => "You win.",
            DuelOutcome.PlayerTwoWins => "Opponent wins.",
            DuelOutcome.Draw => "The duel is a draw.",
            _ when duel.PendingAttack is { } attack && duel.ActivePlayer == Opponent =>
                $"Attack on {(attack.Target is { } unit ? humanView.Units[unit.Slot]!.Card.Name : "your Hero")}.",
            _ => message,
        };
        if (status.Length > 0)
        {
            DrawCenteredText(status, layoutWidth / 2, layoutHeight - 172, 16, Ivory);
        }
    }

    /// <summary>
    /// Draws the hand, with the hovered card on top. A dragged card is drawn by <see cref="DrawDrag"/>.
    /// </summary>
    private void DrawHand()
    {
        for (var i = 0; i < humanView.HandCount; i++)
        {
            if (i != hoveredHand && !(handDrag is { Started: true } drag && drag.Slot == i))
            {
                DrawHandCard(i);
            }
        }
        if (hoveredHand >= 0)
        {
            DrawHandCard(hoveredHand);
        }
    }

    /// <summary>Draws one card of the hand, with a shadow when raised.</summary>
    /// <param name="slot">The hand slot.</param>
    private void DrawHandCard(int slot)
    {
        var bounds = PresentedHandBounds(slot);
        if (handLift[slot] > 0)
        {
            var shadow = bounds;
            shadow.Offset(0, 5);
            shadow.Inflate(2, 2);
            FillRoundedRectangle(shadow, Color.Black * (0.48f * handLift[slot]));
        }
        DrawCard(bounds, humanView.Hand![slot], slot == selectedHand || mulliganSlots.Contains(slot));
    }

    /// <summary>Highlights where the dragged card can go and draws it under the pointer.</summary>
    private void DrawDrag()
    {
        if (handDrag is not { Started: true, Position: { } position } drag)
        {
            return;
        }

        var card = humanView.Hand![drag.Slot];
        foreach (var play in drag.Plays)
        {
            var bounds = DropBounds(card, play);
            var highlight = bounds.Contains(position) ? Gold : Teal;
            if (card.Type == CardType.Spell && play.Target is null)
            {
                DrawBox(bounds, Panel, highlight);
                DrawCenteredText("CAST SPELL", bounds.Center.X, bounds.Y + 12, 19, Ivory);
            }
            else
            {
                DrawOutline(bounds, highlight);
            }
        }

        // The dragged card shrinks slightly, keeping the grabbed point under the pointer.
        var handWidth = HandBounds(drag.Slot, humanView.HandCount).Width;
        var previewWidth = Math.Max(5, (int)(handWidth * 0.8f) / 5 * 5);
        var scale = (float)previewWidth / handWidth;
        var location = position.ToVector2() - drag.Offset.ToVector2() * scale;
        DrawCard(CardBounds((int)location.X, (int)location.Y, previewWidth), card, true);
    }

    /// <summary>Draws the buttons that apply to the current step.</summary>
    private void DrawActions()
    {
        if (duel.Outcome != DuelOutcome.Ongoing)
        {
            DrawButton(AnchorBottomCenter(RematchButton), "Play again", true);
            return;
        }

        if (duel.Phase == DuelPhase.Mulligan)
        {
            DrawButton(AnchorBottomRight(NextButton),
                mulliganSlots.Count == 0 ? "Keep hand" : $"Replace {mulliganSlots.Count} cards", true);
            return;
        }

        if (duel.PendingAttack is not null)
        {
            if (duel.ActivePlayer == Opponent)
            {
                DrawButton(AnchorBottomRight(NextButton), "Take the hit", true);
            }
            return;
        }

        if (duel.ActivePlayer != Human)
        {
            return;
        }

        switch (duel.Phase)
        {
            case DuelPhase.Main:
                DrawButton(AnchorBottomLeft(SparkButton), "Use Spark", humanView.HasSpark);
                DrawButton(AnchorBottomRight(NextButton), "Go to battle", true);
                break;
            case DuelPhase.Battle:
                DrawButton(AnchorBottomRight(NextButton), "End battle", true);
                break;
            case DuelPhase.End:
                DrawButton(AnchorBottomLeft(DiscardButton), "Discard",
                    selectedHand is not null && humanView.HandCount > Duel.HandLimit);
                DrawButton(AnchorBottomRight(NextButton), "End turn", humanView.HandCount <= Duel.HandLimit);
                break;
        }
        if (selectedAttacker is not null || selectedHand is not null)
        {
            DrawButton(AnchorBottomRight(CancelButton), "Cancel selection", true);
        }
    }

    /// <summary>Draws a card face.</summary>
    /// <param name="bounds">The card bounds.</param>
    /// <param name="card">The card.</param>
    /// <param name="selected">Whether to highlight it.</param>
    /// <param name="unit">The Unit in play, or <see langword="null"/> to show printed values.</param>
    /// <param name="enlarged">Whether to allow larger text.</param>
    private void DrawCard(Rectangle bounds, CardDefinition card, bool selected, UnitView? unit = null, bool enlarged = false)
    {
        var accent = card.Type switch
        {
            CardType.Unit => Teal,
            CardType.Spell => Rose,
            _ => Gold,
        };
        var ready = unit is { CanAttack: true } or { CanIntercept: true };
        var fill = unit is { Exhausted: true } ? new Color(33, 41, 44) : new Color(30, 49, 54);
        DrawBox(bounds, fill, selected ? Gold : ready ? Teal : Border);
        var inset = Math.Max(4, bounds.Width / 12);
        var nameFont = Math.Clamp(bounds.Width / 5, 10, enlarged ? 29 : 18);
        var detailFont = Math.Clamp(bounds.Width / 7, 9, enlarged ? 20 : 14);
        var interior = bounds.Width - 2 * inset;
        FillRectangle(new(bounds.X + inset, bounds.Y + 6, interior, 2), selected ? Gold : accent);
        DrawText($"{card.Type.ToString().ToUpperInvariant()} {card.Cost}", bounds.X + inset, bounds.Y + 11,
            detailFont, accent);
        var nameRows = BitmapText.Wrap(card.Name, Math.Max(5, interior / BitmapText.Advance(nameFont)), 2);
        for (var i = 0; i < nameRows.Length; i++)
        {
            DrawText(nameRows[i], bounds.X + inset, bounds.Y + 13 + detailFont + i * nameFont, nameFont, Ivory);
        }

        var descriptionRows = BitmapText.Wrap(CardRulesText.Summarize(card),
            Math.Max(5, interior / BitmapText.Advance(detailFont)), 2);
        for (var i = 0; i < descriptionRows.Length; i++)
        {
            var y = bounds.Y + 15 + detailFont + nameFont * 2 + i * detailFont;
            if (y + detailFont <= bounds.Bottom - detailFont - 7)
            {
                DrawText(descriptionRows[i], bounds.X + inset, y, detailFont, Muted);
            }
        }

        if (card.Type == CardType.Unit)
        {
            DrawText($"{unit?.Power ?? card.Power}/{card.Health - (unit?.Damage ?? 0)}",
                bounds.X + inset, bounds.Bottom - nameFont - 5, nameFont, Ivory);
        }
        if (ready || unit is { Exhausted: true })
        {
            var label = ready ? unit!.CanIntercept ? "BLOCK" : "ATTACK" : "USED";
            var labelSize = Math.Max(9, detailFont - 1);
            DrawText(label, bounds.Right - inset - BitmapText.Advance(labelSize) * label.Length,
                bounds.Bottom - labelSize - 6, labelSize, ready ? Teal : Muted);
        }
    }

    /// <summary>Draws an empty board slot.</summary>
    /// <param name="bounds">The slot bounds.</param>
    /// <param name="label">The slot's name.</param>
    private void DrawEmptySlot(Rectangle bounds, string label)
    {
        FillRoundedRectangle(bounds, Recess);
        DrawDashedBorder(bounds, new Color(72, 104, 112));
        DrawCenteredText(label, bounds.Center.X, bounds.Center.Y - 9, 16, Muted);
    }
}
