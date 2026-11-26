using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheCards.Core;

namespace TheCards.Desktop;

/// <summary>Renders the duel and routes single-player clicks to the rules engine.</summary>
internal sealed class BoardUI
{
    public const int Width = 1280;
    public const int Height = 720;
    public const int HeroPortraitSize = 176;
    public const int HeroHealthSize = 36;
    private const int HealthAtlasColumns = 6;
    private const int HealthAtlasCount = 21;
    // Card-shaped counters (Removed and Discard on the left, Deck on the right) flank the Hero portrait.
    // Both dimensions are odd so a digit with an odd size sits exactly in the middle (25 x 35 is 5:7 like a card).
    private const int CounterWidth = 25;
    private const int CounterHeight = 35;
    private const int CounterDigitHeight = 15;
    private const int CounterGap = 14;
    private const int CounterEdge = 24;
    // The portrait is round, so it tucks in until its rim is 5 pixels from the nearest corner of the
    // Removed and Discard counters. Deck mirrors that on the right.
    private const int PortraitClearance = 5;
    private static readonly int PortraitLeft = ComputePortraitLeft();
    private static readonly int DeckCounterLeft = 2 * (PortraitLeft + HeroPortraitSize / 2) - (CounterEdge + CounterWidth);
    private static readonly int HandAreaLeft = DeckCounterLeft + CounterWidth + CounterGap;
    private const int HandAreaRight = Width - 24;
    private const string HealthTooltip =
        "HEALTH|Your Hero's remaining Health. Units and effects reduce it, and you lose at 0.";
    private const string WellTooltip =
        "WELL|Shards in your Well. Each one refills at the start of your turn and pays 1 energy. Once per turn, drag a card onto your portrait to add a shard.";
    private const string DiscardTooltip =
        "DISCARD|Cards in your discard pile: used Spells, destroyed Units, and cards discarded to the hand limit.";
    private const string DeckTooltip =
        "DECK|Cards left in your deck. You draw one at the start of each turn, and you lose if you must draw from an empty deck.";
    private const string RemovedTooltip =
        "REMOVED|Cards removed from the game. They can't return. No cards use this yet.";
    public static readonly Color BackgroundColor = new(12, 21, 27);

    private const int Human = 0;
    private const int Opponent = SinglePlayerOpponent.Side;
    private static readonly Color Ivory = new(240, 234, 219);
    private static readonly Color Muted = new(163, 176, 179);
    private static readonly Color Teal = new(101, 208, 189);
    private static readonly Color Gold = new(235, 184, 107);
    private static readonly Color Rose = new(233, 145, 146);
    private static readonly Color Panel = new(25, 39, 45);
    private static readonly Color Border = new(57, 82, 90);
    private static readonly Rectangle RematchButton = new(510, 502, 260, 40);
    private static readonly Rectangle SparkButton = new(328, 505, 124, 38);
    private static readonly Rectangle DiscardButton = new(460, 505, 125, 38);
    private static readonly Rectangle NextButton = new(1018, 505, 234, 38);
    private static readonly Rectangle CancelButton = new(858, 505, 152, 38);

    private static readonly Rectangle TutorialPanel = new(230, 118, 820, 484);
    private static readonly Rectangle TutorialBack = new(266, 528, 112, 40);
    private static readonly Rectangle TutorialSkip = new(648, 528, 151, 40);
    private static readonly Rectangle TutorialNext = new(813, 528, 201, 40);

    private static readonly Rectangle InspectorPanel = new(610, 82, 580, 258);
    private static readonly Rectangle InspectorClose = new(1090, 90, 84, 29);
    private const int InspectorVisibleRows = 6;

    private static readonly CardDefinition TutorialUnit =
        StarterDecks.Create().First(card => card.Id == "mistwing-scout");

    private readonly SpriteBatch batch;
    private readonly Texture2D pixel;
    private readonly Texture2D font;
    private readonly Texture2D heroPortrait;
    private readonly Texture2D opponentPortrait;
    private readonly Texture2D heroPlaymat;
    private readonly Texture2D opponentPlaymat;
    private readonly Texture2D heroHealth;
    private readonly Texture2D heroDropRing;
    private readonly Texture2D badgeBlank;
    private readonly Texture2D[] counterDigits;
    private readonly HashSet<int> mulligan = [];
    private readonly float[] handLift = new float[Duel.DeckSize];
    private int hoveredHand = -1;
    private CardDefinition? inspectedCard;
    private string[] inspectorRows = [];
    private int inspectorScroll;
    private int layoutHeight = Height;
    private int horizontalPadding;

    private int ExtraHeight => layoutHeight - Height;
    private int TutorialOffset => ExtraHeight / 2;
    private BoardTutorial tutorial = new();
    private Duel duel;
    private PlayerView own;
    private PlayerView enemy;
    private int? selectedHand;
    private int? selectedAttacker;
    private HandDrag? handDrag;
    private double computerDelay;
    private string message = "";
    private Point? hoverPointer;

    public BoardUI(SpriteBatch batch, Texture2D pixel, Texture2D font,
        Texture2D heroPortrait, Texture2D heroHealth, Texture2D heroDropRing, Texture2D badgeBlank,
        Texture2D counterDigitSheet, Texture2D heroPlaymat, Texture2D opponentPortrait, Texture2D opponentPlaymat)
    {
        this.batch = batch;
        this.pixel = pixel;
        this.font = font;
        this.heroPortrait = heroPortrait;
        this.opponentPortrait = opponentPortrait;
        this.heroPlaymat = heroPlaymat;
        this.opponentPlaymat = opponentPlaymat;
        this.heroHealth = heroHealth;
        this.heroDropRing = heroDropRing;
        this.badgeBlank = badgeBlank;
        counterDigits = BuildCounterDigits(counterDigitSheet);
        duel = NewDuel();
        own = duel.View(Human, Human);
        enemy = duel.View(Opponent, Human);
    }

    private static int ComputePortraitLeft()
    {
        // Removed's lower inner corner is (HeroPortraitSize / 2 - CounterHeight) above the portrait's center line.
        var radius = HeroPortraitSize / 2;
        var vertical = radius - CounterHeight;
        var horizontal = (int)MathF.Ceiling(MathF.Sqrt((radius + PortraitClearance) * (radius + PortraitClearance)
            - vertical * vertical));
        return CounterEdge + CounterWidth + horizontal - radius;
    }

    public void Dispose()
    {
        foreach (var digit in counterDigits)
        {
            digit.Dispose();
        }
    }

    /// <summary>
    /// Shrinks the regular-weight sans-serif digits 0-9 from a one-row sheet to counter size with an area
    /// average. Every digit texture has the same height and an odd width, so numbers share a baseline and,
    /// in a counter with odd dimensions, center exactly.
    /// </summary>
    private static Texture2D[] BuildCounterDigits(Texture2D sheet)
    {
        var cellWidth = sheet.Width / 10;
        var cellHeight = sheet.Height;
        var pixels = new Color[sheet.Width * sheet.Height];
        sheet.GetData(pixels);
        var glyphs = new Rectangle[10];
        int top = int.MaxValue, bottom = int.MinValue;
        for (var digit = 0; digit < glyphs.Length; digit++)
        {
            int minX = cellWidth, minY = cellHeight, maxX = -1, maxY = -1;
            for (var y = 0; y < cellHeight; y++)
            {
                for (var x = 0; x < cellWidth; x++)
                {
                    if (pixels[y * sheet.Width + digit * cellWidth + x].R < 24)
                    {
                        continue;
                    }
                    minX = Math.Min(minX, x);
                    maxX = Math.Max(maxX, x);
                    minY = Math.Min(minY, y);
                    maxY = Math.Max(maxY, y);
                }
            }
            glyphs[digit] = new Rectangle(digit * cellWidth + minX, minY, maxX - minX + 1, maxY - minY + 1);
            top = Math.Min(top, minY);
            bottom = Math.Max(bottom, maxY + 1);
        }

        var scale = (float)CounterDigitHeight / (bottom - top);
        var textures = new Texture2D[glyphs.Length];
        for (var digit = 0; digit < glyphs.Length; digit++)
        {
            var glyph = glyphs[digit];
            // Round the width to the nearest odd number.
            var width = Math.Max(1, (int)MathF.Round((glyph.Width * scale - 1) / 2) * 2 + 1);
            var output = new Color[width * CounterDigitHeight];
            for (var y = 0; y < CounterDigitHeight; y++)
            {
                var sourceTop = top + y / scale;
                var sourceBottom = top + (y + 1) / scale;
                for (var x = 0; x < width; x++)
                {
                    var sourceLeft = glyph.X + x * glyph.Width / (float)width;
                    var sourceRight = glyph.X + (x + 1) * glyph.Width / (float)width;
                    float sum = 0, weight = 0;
                    for (var sy = (int)MathF.Floor(sourceTop); sy < (int)MathF.Ceiling(sourceBottom); sy++)
                    {
                        for (var sx = (int)MathF.Floor(sourceLeft); sx < (int)MathF.Ceiling(sourceRight); sx++)
                        {
                            if (sx >= glyph.X && sx < glyph.Right && sy >= 0 && sy < cellHeight)
                            {
                                sum += pixels[sy * sheet.Width + sx].R;
                                weight++;
                            }
                        }
                    }
                    var coverage = weight == 0 ? 0 : sum / weight / 255f;
                    output[y * width + x] = new Color(coverage, coverage, coverage, coverage);
                }
            }
            textures[digit] = new Texture2D(sheet.GraphicsDevice, width, CounterDigitHeight);
            textures[digit].SetData(output);
        }
        return textures;
    }

    public void SetLayout(BoardViewport viewport)
    {
        layoutHeight = viewport.Height;
        horizontalPadding = viewport.HorizontalPadding;
    }

    private static Duel NewDuel()
    {
        var match = new Duel(StarterDecks.Create(), StarterDecks.Create(secondDeck: true));
        match.Mulligan(Opponent, []);
        return match;
    }

    private void Rematch()
    {
        duel = NewDuel();
        tutorial = new BoardTutorial();
        mulligan.Clear();
        ResetSelection();
        ResetHandLift();
        computerDelay = 0;
        message = "";
        Refresh();
    }

    private void Refresh()
    {
        var next = duel.View(Human, Human);
        if (next.HandCount != own.HandCount)
        {
            ResetHandLift();
        }
        own = next;
        enemy = duel.View(Opponent, Human);
    }

    public void Update(TimeSpan elapsed, Point? pointer = null)
    {
        hoverPointer = pointer;
        if (tutorial.IsOpen)
        {
            return;
        }

        MoveDrag(pointer);
        UpdateHandLift(elapsed, pointer);

        while (duel.Phase is DuelPhase.Ready or DuelPhase.Draw)
        {
            duel.Advance(duel.ActivePlayer);
            Refresh();
        }

        if (duel.Outcome != DuelOutcome.Ongoing || duel.Phase == DuelPhase.Mulligan ||
            (duel.PendingAttack is null ? duel.ActivePlayer != Opponent : duel.ActivePlayer != Human))
        {
            computerDelay = 0;
            return;
        }

        computerDelay += elapsed.TotalSeconds;
        if (computerDelay < 0.36)
        {
            return;
        }

        computerDelay = 0;
        var update = SinglePlayerOpponent.TakeStep(duel);
        if (update.Length > 0)
        {
            message = update;
        }
        Refresh();
    }

    public void PointerPressed(Point position)
    {
        var slot = !tutorial.IsOpen && duel.Phase == DuelPhase.Main && duel.ActivePlayer == Human
            ? FindHand(position) : -1;
        if (slot >= 0)
        {
            handDrag = new HandDrag(slot, position, position - PresentedHandBounds(slot).Location);
            return;
        }

        Click(position);
    }

    public void PointerReleased(Point? position)
    {
        if (handDrag is not { } drag)
        {
            return;
        }

        MoveDrag(position);
        handDrag = null;
        if (position is not { } point)
        {
            return;
        }
        if (!drag.Started)
        {
            Click(drag.Start);
            return;
        }

        if (IsOverHeroPortrait(point))
        {
            if (own.CanFeedWell)
            {
                duel.FeedWell(Human, drag.Slot);
                message = "Added a face-down shard to your Well.";
                ResetSelection();
                Refresh();
            }
            else
            {
                message = "Your Well cannot accept another card this turn.";
            }
            return;
        }

        var card = own.Hand![drag.Slot];
        foreach (var play in drag.Plays)
        {
            if (!DropBounds(card, play).Contains(point))
            {
                continue;
            }

            try
            {
                duel.Play(Human, drag.Slot, play.Target, play.BoardSlot);
                message = $"You played {card.Name}.";
                ResetSelection();
                Refresh();
            }
            catch (InvalidOperationException error)
            {
                message = error.Message;
            }
            return;
        }
        message = drag.Plays.Count == 0 && !own.CanFeedWell
            ? "No legal destination for that card." : "Drop the card on a highlighted space.";
    }

    public void CancelDrag() => handDrag = null;

    public void RightClick(Point position)
    {
        var slot = !tutorial.IsOpen && handDrag is null ? FindHand(position) : -1;
        if (slot >= 0)
        {
            Inspect(own.Hand![slot]);
        }
    }

    private void MoveDrag(Point? pointer)
    {
        if (handDrag is not { } drag)
        {
            return;
        }

        drag.Position = pointer;
        if (!drag.Started && pointer is { } position &&
            Vector2.DistanceSquared(position.ToVector2(), drag.Start.ToVector2()) >= 36f)
        {
            drag.Started = true;
            drag.Plays = duel.LegalPlays(Human, drag.Slot);
            CloseInspector();
        }
    }

    private Rectangle DropBounds(CardDefinition card, CardPlay play)
    {
        if (play.Target is { } target)
        {
            return UnitBounds(target.Player, target.Slot);
        }
        return card.Type switch
        {
            CardType.Unit => UnitBounds(Human, play.BoardSlot!.Value),
            CardType.Relic => RelicBounds(Human, play.BoardSlot!.Value),
            _ => new Rectangle(1026, 283 + ExtraHeight / 2, 210, 46),
        };
    }

    public void Click(Point position)
    {
        if (tutorial.IsOpen)
        {
            if (InTutorial(TutorialNext).Contains(position))
            {
                tutorial.Next();
            }
            else if (InTutorial(TutorialSkip).Contains(position))
            {
                tutorial.Close();
            }
            else if (!tutorial.IsPrompt && InTutorial(TutorialBack).Contains(position))
            {
                tutorial.Back();
            }
            return;
        }

        if (inspectedCard is not null && InspectorPanel.Contains(position))
        {
            if (InspectorClose.Contains(position))
            {
                CloseInspector();
            }
            return;
        }

        if (duel.Outcome != DuelOutcome.Ongoing)
        {
            if (AtBottom(RematchButton).Contains(position))
            {
                Rematch();
            }
            return;
        }

        try
        {
            HandleClick(position);
            Refresh();
        }
        catch (InvalidOperationException error)
        {
            message = error.Message;
        }
    }

    private void HandleClick(Point position)
    {
        var handSlot = FindHand(position);
        if (handSlot >= 0)
        {
            if (duel.Phase == DuelPhase.Mulligan && !mulligan.Add(handSlot))
            {
                mulligan.Remove(handSlot);
            }
            if (duel.ActivePlayer == Human && duel.Phase == DuelPhase.End)
            {
                selectedHand = handSlot;
                message = "";
            }
            return;
        }

        if (duel.Phase == DuelPhase.Mulligan)
        {
            if (AtBottom(NextButton).Contains(position))
            {
                duel.Mulligan(Human, mulligan);
                message = duel.ActivePlayer == Human ? "You go first." : "You go second and receive a Spark.";
                mulligan.Clear();
                CloseInspector();
                ResetHandLift();
            }
            return;
        }

        if (duel.PendingAttack is not null)
        {
            if (duel.ActivePlayer == Opponent)
            {
                var unit = FindUnit(Human, position);
                if (AtBottom(NextButton).Contains(position))
                {
                    duel.Defend(Human);
                    message = "Attack resolved.";
                }
                else if (unit >= 0 && own.Units[unit]?.CanIntercept == true)
                {
                    var name = own.Units[unit]!.Card.Name;
                    duel.Defend(Human, unit);
                    message = $"{name} intercepted.";
                }
            }
            return;
        }

        if (duel.ActivePlayer != Human)
        {
            return;
        }

        if (AtBottom(CancelButton).Contains(position))
        {
            ResetSelection();
            message = "";
            return;
        }

        switch (duel.Phase)
        {
            case DuelPhase.Main:
                HandleMain(position);
                break;
            case DuelPhase.Battle:
                HandleBattle(position);
                break;
            case DuelPhase.End:
                if (AtBottom(DiscardButton).Contains(position) && selectedHand is { } discard && own.HandCount > Duel.HandLimit)
                {
                    duel.DiscardToLimit(Human, discard);
                    ResetSelection();
                }
                if (AtBottom(NextButton).Contains(position) && own.HandCount <= Duel.HandLimit)
                {
                    duel.Advance(Human);
                    ResetSelection();
                }
                break;
        }
    }

    private void HandleMain(Point position)
    {
        if (AtBottom(NextButton).Contains(position))
        {
            duel.Advance(Human);
            ResetSelection();
            return;
        }
        if (AtBottom(SparkButton).Contains(position) && own.HasSpark)
        {
            duel.UseSpark(Human);
            message = "Spark added 1 temporary energy.";
            return;
        }
    }

    private void HandleBattle(Point position)
    {
        if (AtBottom(NextButton).Contains(position))
        {
            duel.Advance(Human);
            ResetSelection();
            return;
        }

        var ownSlot = FindUnit(Human, position);
        if (ownSlot >= 0 && own.Units[ownSlot]?.CanAttack == true)
        {
            selectedAttacker = selectedAttacker == ownSlot ? null : ownSlot;
            CloseInspector();
            return;
        }

        if (selectedAttacker is not int attacker)
        {
            return;
        }

        var enemySlot = FindUnit(Opponent, position);
        if (enemySlot >= 0 && enemy.Units[enemySlot] is not null)
        {
            duel.Attack(Human, attacker, new(Opponent, enemySlot));
        }
        else if (InPortrait(PortraitBounds(Opponent), position))
        {
            duel.Attack(Human, attacker);
        }
        else
        {
            return;
        }
        message = $"{own.Units[attacker]!.Card.Name} attacked.";
        ResetSelection();
    }

    private void Inspect(CardDefinition card)
    {
        inspectedCard = card;
        inspectorRows = CardRulesText.Describe(card).SelectMany(rule => Wrap(rule, 42, int.MaxValue)).ToArray();
        inspectorScroll = 0;
    }

    private void CloseInspector() => inspectedCard = null;

    public void ScrollInspector(Point position, int direction)
    {
        if (!tutorial.IsOpen && inspectedCard is not null && InspectorPanel.Contains(position))
        {
            inspectorScroll = Math.Clamp(inspectorScroll + direction * 2, 0,
                Math.Max(0, inspectorRows.Length - InspectorVisibleRows));
        }
    }

    private void ResetSelection()
    {
        selectedHand = null;
        selectedAttacker = null;
        CancelDrag();
        CloseInspector();
    }

    private void ResetHandLift()
    {
        Array.Clear(handLift);
        hoveredHand = -1;
    }

    private void UpdateHandLift(TimeSpan elapsed, Point? pointer)
    {
        hoveredHand = handDrag is not { Started: true } && pointer is { } position ? FindHand(position) : -1;
        var step = (float)Math.Min(elapsed.TotalSeconds / 0.16, 1);
        for (var i = 0; i < own.HandCount; i++)
        {
            handLift[i] = i == hoveredHand
                ? Math.Min(1f, handLift[i] + step)
                : Math.Max(0f, handLift[i] - step);
        }
    }

    private int FindHand(Point position)
    {
        for (var i = 0; i < own.HandCount; i++)
        {
            if (Rectangle.Union(HandBounds(i, own.HandCount), PresentedHandBounds(i)).Contains(position))
            {
                return i;
            }
        }
        return -1;
    }

    private Rectangle PresentedHandBounds(int slot)
    {
        var bounds = HandBounds(slot, own.HandCount);
        var lift = MathHelper.SmoothStep(0f, 1f, handLift[slot]);
        bounds.Offset(0, -(int)MathF.Round((bounds.Height / 2f + 14f) * lift));
        return bounds;
    }

    private int FindUnit(int player, Point position)
    {
        for (var i = 0; i < Duel.UnitSlots; i++)
        {
            if (UnitBounds(player, i).Contains(position))
            {
                return i;
            }
        }
        return -1;
    }

    private Rectangle AtBottom(Rectangle bounds)
    {
        bounds.Offset(0, ExtraHeight);
        return bounds;
    }

    private Rectangle InTutorial(Rectangle bounds)
    {
        bounds.Offset(0, TutorialOffset);
        return bounds;
    }

    private Rectangle AcrossWindow(int y, int height) =>
        new(-horizontalPadding, y, Width + 2 * horizontalPadding, height);

    private int SideTop(int player) => player == Human
        ? 352 + ExtraHeight * 3 / 4
        : 122 + ExtraHeight / 4;

    // The portrait sits in the lower-left corner, low enough that its Health badge stays inside the window.
    private Rectangle HeroPortraitBounds() => new(PortraitLeft, layoutHeight - HeroPortraitSize - HeroHealthSize / 2 - 10,
        HeroPortraitSize, HeroPortraitSize);

    private Rectangle PortraitBounds(int player) => player == Human ? HeroPortraitBounds()
        : new(PortraitLeft, HeroHealthSize / 2 + 10, HeroPortraitSize, HeroPortraitSize);

    /// <summary>Returns a badge centered on the portrait's rim at a clock position (12 is the top).</summary>
    private static Rectangle RimBadge(Rectangle portrait, float hour)
    {
        var angle = hour / 12f * MathHelper.TwoPi;
        var radius = HeroPortraitSize / 2f;
        var x = portrait.Center.X + (int)MathF.Round(radius * MathF.Sin(angle));
        var y = portrait.Center.Y - (int)MathF.Round(radius * MathF.Cos(angle));
        return new(x - HeroHealthSize / 2, y - HeroHealthSize / 2, HeroHealthSize, HeroHealthSize);
    }

    private (Rectangle Bounds, int Value, string Tooltip)[] PortraitBadges(int player, PlayerView view)
    {
        var portrait = PortraitBounds(player);
        return
        [
            (RimBadge(portrait, 12), view.Shards, player == Human ? WellTooltip :
                "WELL|Shards in the Opponent's Well. Each ready shard pays for 1 energy."),
            (RimBadge(portrait, 6), view.Health, player == Human ? HealthTooltip :
                "OPPONENT|Their remaining Health. Attack this portrait to reduce it."),
        ];
    }

    private static (Rectangle Bounds, int Value, string Tooltip)[] CardCounters(Rectangle portrait, PlayerView view)
    {
        var bottom = portrait.Bottom - CounterHeight;
        return
        [
            (new(CounterEdge, portrait.Top, CounterWidth, CounterHeight), 0, RemovedTooltip),
            (new(CounterEdge, bottom, CounterWidth, CounterHeight), view.Discard.Count, DiscardTooltip),
            (new(DeckCounterLeft, bottom, CounterWidth, CounterHeight), view.DeckCount, DeckTooltip),
        ];
    }

    private static bool InBadge(Rectangle badge, Point position)
    {
        var delta = position - badge.Center;
        return delta.X * delta.X + delta.Y * delta.Y <= HeroHealthSize * HeroHealthSize / 4;
    }

    private void DrawCardCounter(Rectangle bounds, int value)
    {
        Box(bounds, Color.Black, Color.White);
        const int gap = 1; // With odd digit widths this keeps the whole number's width odd.
        var label = Math.Max(0, value).ToString(System.Globalization.CultureInfo.InvariantCulture);
        var digits = label.Select(character => counterDigits[character - '0']).ToArray();
        var total = digits.Sum(digit => digit.Width) + gap * (digits.Length - 1);
        var x = bounds.X + (bounds.Width - total) / 2;
        var y = bounds.Y + (bounds.Height - CounterDigitHeight) / 2;
        foreach (var digit in digits)
        {
            batch.Draw(digit, new Vector2(x, y), Color.White);
            x += digit.Width + gap;
        }
    }

    private bool IsOverHeroPortrait(Point position) => InPortrait(HeroPortraitBounds(), position);

    private static bool InPortrait(Rectangle bounds, Point position)
    {
        var delta = position - bounds.Center;
        return delta.X * delta.X + delta.Y * delta.Y <= HeroPortraitSize * HeroPortraitSize / 4;
    }

    private Rectangle UnitBounds(int player, int slot) => CardBounds(265 + slot * 150, SideTop(player));
    private Rectangle RelicBounds(int player, int slot) => CardBounds(1008 + slot * 131, SideTop(player));

    private static Rectangle CardBounds(int x, int y, int width = 95) =>
        new(x, y, width, width / 5 * 7);

    private Rectangle HandBounds(int slot, int count)
    {
        const int spacing = 8;
        // Cards are centered in the space to the right of the Hero portrait.
        var available = HandAreaRight - HandAreaLeft;
        var width = Math.Min(95, (available - spacing * (count - 1)) / Math.Max(1, count));
        width = Math.Max(5, width / 5 * 5);
        var total = width * count + spacing * (count - 1);
        return CardBounds(HandAreaLeft + (available - total) / 2 + (width + spacing) * slot,
            layoutHeight - width / 5 * 7 / 2, width);
    }

    public void Draw(BoardViewport view, RasterizerState rasterizer)
    {
        SetLayout(view);
        batch.Begin(samplerState: SamplerState.LinearClamp, rasterizerState: rasterizer,
            transformMatrix: view.Transform);
        Fill(AcrossWindow(0, layoutHeight), BackgroundColor);
        var split = layoutHeight / 2 - 22;
        DrawPlaymat(opponentPlaymat, AcrossWindow(0, split), verticalAnchor: 0.5f);
        DrawPlaymat(heroPlaymat, AcrossWindow(split, layoutHeight - split), verticalAnchor: 1f);
        DrawSide(Opponent, enemy);
        DrawSide(Human, own);
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
        batch.End();
    }

    private void DrawPlaymat(Texture2D texture, Rectangle bounds, float verticalAnchor)
    {
        var scale = Math.Max((float)bounds.Width / texture.Width, (float)bounds.Height / texture.Height);
        var width = Math.Clamp((int)MathF.Round(bounds.Width / scale), 1, texture.Width);
        var height = Math.Clamp((int)MathF.Round(bounds.Height / scale), 1, texture.Height);
        var source = new Rectangle((texture.Width - width) / 2,
            (int)MathF.Round((texture.Height - height) * verticalAnchor), width, height);
        batch.Draw(texture, bounds, source, Color.White);
    }

    private void DrawSide(int player, PlayerView view)
    {
        var portrait = PortraitBounds(player);
        var attackTarget = player == Opponent && selectedAttacker is not null;
        batch.Draw(player == Human ? heroPortrait : opponentPortrait, portrait, Color.White);
        if (attackTarget || (player == Human && own.CanFeedWell && handDrag is { Started: true, Position: not null }))
        {
            var ring = portrait;
            ring.Inflate(3, 3);
            var hovered = handDrag?.Position is { } pointer && InPortrait(portrait, pointer);
            batch.Draw(heroDropRing, ring, attackTarget || hovered ? Gold : Teal);
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
            Text("SPARK READY", 28, statusY, 12, Gold);
        }
        if (view.TemporaryEnergy > 0)
        {
            Text($"+{view.TemporaryEnergy} ENERGY", 28, statusY + 18, 12, Gold);
        }

        for (var i = 0; i < Duel.UnitSlots; i++)
        {
            var bounds = UnitBounds(player, i);
            if (view.Units[i] is { } unit)
            {
                var isAttacker = player == Human && selectedAttacker == i;
                var highlight = isAttacker || (attackTarget && player == Opponent);
                DrawCard(bounds, unit.Card, highlight, unit);
            }
            else
            {
                DashedSlot(bounds);
                CenterText($"UNIT {i + 1}", bounds.Center.X, bounds.Center.Y - 9, 16, Muted);
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
                DashedSlot(bounds);
                CenterText($"RELIC {i + 1}", bounds.Center.X, bounds.Center.Y - 9, 16, Muted);
            }
        }
    }

    /// <summary>Draws a number from the round badge atlas shared by the portrait counters.</summary>
    private void DrawBadge(Rectangle bounds, int value)
    {
        if (value >= HealthAtlasCount)
        {
            // The atlas stops at 20, so larger numbers are drawn as text on a blank badge.
            batch.Draw(badgeBlank, bounds, Color.White);
            var label = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            CenterText(label, bounds.Center.X, bounds.Center.Y - 9, 18, Color.White);
            return;
        }

        var index = Math.Max(0, value);
        var cellSize = heroHealth.Width / HealthAtlasColumns;
        var tile = new Rectangle(index % HealthAtlasColumns * cellSize, index / HealthAtlasColumns * cellSize,
            cellSize, cellSize);
        batch.Draw(heroHealth, bounds, tile, Color.White);
    }

    private void DrawBadgeTooltip()
    {
        if (handDrag is { Started: true } || hoverPointer is not { } pointer)
        {
            return;
        }

        var player = pointer.Y < layoutHeight / 2 ? Opponent : Human;
        var view = player == Human ? own : enemy;
        var portrait = PortraitBounds(player);
        var counters = CardCounters(portrait, view);
        var hovered = PortraitBadges(player, view).Where(badge => InBadge(badge.Bounds, pointer))
            .Concat(counters.Where(counter => counter.Bounds.Contains(pointer))).ToArray();
        if (hovered.Length == 0)
        {
            return;
        }

        var (badge, _, tooltip) = hovered[0];
        var deck = counters[2].Bounds;

        var separator = tooltip.IndexOf('|');
        var heading = tooltip[..separator];
        var lines = Wrap(tooltip[(separator + 1)..], 34, int.MaxValue);
        const int padding = 12;
        const int bodySize = 16;
        var width = 2 * padding + TextAdvance(bodySize) * Math.Max(heading.Length, lines.Max(line => line.Length));
        var height = 2 * padding + 20 + lines.Length * (bodySize + 4);
        // Show it beside the portrait, level with the badge, so it never covers the portrait.
        var y = Math.Clamp(badge.Center.Y - height / 2, 8, layoutHeight - height - 8);
        var bounds = new Rectangle(Math.Max(badge.Right, portrait.Right) + 10, y, width, height);
        if (bounds.Intersects(deck) && badge != deck)
        {
            // Keep the tooltip from covering the Deck counter.
            bounds.X = deck.Right + 10;
        }
        Box(bounds, new Color(19, 34, 43), new Color(73, 110, 120));
        Text(heading, bounds.X + padding, bounds.Y + padding, 15, Teal);
        for (var i = 0; i < lines.Length; i++)
        {
            Text(lines[i], bounds.X + padding, bounds.Y + padding + 20 + i * (bodySize + 4), bodySize, Ivory);
        }
    }

    private void DrawTutorial()
    {
        Fill(AcrossWindow(0, layoutHeight), new Color(0, 0, 0) * 0.75f);
        Box(InTutorial(TutorialPanel), new Color(19, 34, 43), new Color(73, 110, 120));
        var left = TutorialPanel.X + 36;
        if (tutorial.IsPrompt)
        {
            Text("BEFORE THE DUEL", left, 161 + TutorialOffset, 21, Teal);
            Text("Want to learn the rules?", left, 207 + TutorialOffset, 43, Ivory);
            Text("Learn the board layout, card details, and rules of a duel.", left, 280 + TutorialOffset, 23, Muted);
            Text("You can skip the guide and go straight to your opening hand.", left, 325 + TutorialOffset, 21, Muted);
            Button(InTutorial(TutorialSkip), "Skip for now", false, quiet: true);
            Button(InTutorial(TutorialNext), "Learn the rules", true);
            return;
        }

        var lesson = BoardTutorial.Pages[tutorial.PageIndex];
        Text($"HOW TO PLAY  /  {tutorial.PageIndex + 1} OF {BoardTutorial.Pages.Count}", left, 151 + TutorialOffset, 19, Teal);
        Text(lesson.Title, left, 191 + TutorialOffset, 37, Ivory);
        var pointsX = left;
        var lineWidth = 64;
        switch (lesson.Illustration)
        {
            case TutorialIllustration.Board:
                DrawTutorialBoard(left, 254 + TutorialOffset);
                pointsX += 186;
                lineWidth = 48;
                break;
            case TutorialIllustration.Card:
                var example = CardBounds(left + 12, 245 + TutorialOffset, 135);
                DrawCard(example, TutorialUnit, false, new(TutorialUnit, TutorialUnit.Power, 1, false, true, false));
                CenterText("EXAMPLE UNIT", example.Center.X, example.Bottom + 8, 14, Muted);
                pointsX += 186;
                lineWidth = 48;
                break;
        }
        DrawTutorialPoints(lesson.Points, pointsX, lineWidth);

        var progressY = 503 + TutorialOffset;
        for (var i = 0; i < BoardTutorial.Pages.Count; i++)
        {
            Fill(new(left + i * 30, progressY, 23, 3), i <= tutorial.PageIndex ? Teal : Border);
        }
        Button(InTutorial(TutorialBack), "Back", false, quiet: true);
        Button(InTutorial(TutorialSkip), "Skip guide", false, quiet: true);
        Button(InTutorial(TutorialNext), tutorial.IsLastPage ? "Start duel" : "Next", true);
    }

    private void DrawTutorialPoints(IReadOnlyList<string> points, int left, int lineWidth)
    {
        var y = 250 + TutorialOffset;
        foreach (var point in points)
        {
            Fill(new(left + 1, y + 8, 5, 5), Teal);
            foreach (var line in Wrap(point, lineWidth, 5))
            {
                Text(line, left + 18, y, 21, Ivory);
                y += 23;
            }
            y += 10;
        }
    }

    private void DrawTutorialBoard(int left, int top)
    {
        const int width = 160;
        Box(new(left, top, width, 199), new Color(14, 25, 31), Border);
        CenterText("EXIT / SOUND", left + width / 2, top + 9, 14, Muted);
        for (var side = 0; side < 2; side++)
        {
            var y = top + 36 + side * 77;
            var accent = side == 0 ? Rose : Teal;
            Text(side == 0 ? "OPPONENT" : "YOU", left + 11, y, 14, accent);
            Fill(new(left + 12, y + 19, 19, 24), accent);
            for (var slot = 0; slot < Duel.UnitSlots + Duel.RelicSlots; slot++)
            {
                var x = left + 40 + slot * 15 + (slot >= Duel.UnitSlots ? 3 : 0);
                Fill(new(x, y + 23, 11, 16), slot < Duel.UnitSlots ? Border : Gold * 0.65f);
            }
        }
        CenterText("ACTIONS", left + width / 2, top + 159, 14, Muted);
        CenterText("YOUR HAND", left + width / 2, top + 177, 14, Teal);
    }

    private void DrawStatus()
    {
        var status = duel.Outcome switch
        {
            DuelOutcome.PlayerOneWins => "You win.",
            DuelOutcome.PlayerTwoWins => "Opponent wins.",
            DuelOutcome.Draw => "The duel is a draw.",
            _ when duel.PendingAttack is { } attack && duel.ActivePlayer == Opponent =>
                $"Attack on {(attack.Target is { } unit ? own.Units[unit.Slot]!.Card.Name : "your Hero")}.",
            _ => message,
        };
        if (status.Length > 0)
        {
            CenterText(status, Width / 2, layoutHeight - 172, 16, Ivory);
        }
    }

    private void DrawHand()
    {
        for (var i = 0; i < own.HandCount; i++)
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

    private void DrawHandCard(int slot)
    {
        var bounds = PresentedHandBounds(slot);
        if (handLift[slot] > 0)
        {
            var shadow = bounds;
            shadow.Offset(0, 5);
            shadow.Inflate(2, 2);
            RoundedFill(shadow, Color.Black * (0.48f * handLift[slot]));
        }
        DrawCard(bounds, own.Hand![slot], slot == selectedHand || mulligan.Contains(slot));
    }

    private void DrawDrag()
    {
        if (handDrag is not { Started: true, Position: { } position } drag)
        {
            return;
        }

        var card = own.Hand![drag.Slot];
        foreach (var play in drag.Plays)
        {
            var bounds = DropBounds(card, play);
            var highlight = bounds.Contains(position) ? Gold : Teal;
            if (card.Type == CardType.Spell && play.Target is null)
            {
                Box(bounds, Panel, highlight);
                CenterText("CAST SPELL", bounds.Center.X, bounds.Y + 12, 19, Ivory);
            }
            else
            {
                DrawDropOutline(bounds, highlight);
            }
        }

        var handWidth = HandBounds(drag.Slot, own.HandCount).Width;
        var previewWidth = Math.Max(5, (int)(handWidth * 0.8f) / 5 * 5);
        var scale = (float)previewWidth / handWidth;
        var location = position.ToVector2() - drag.Offset.ToVector2() * scale;
        DrawCard(CardBounds((int)location.X, (int)location.Y, previewWidth), card, true);

    }

    private void DrawDropOutline(Rectangle bounds, Color highlight)
    {
        Fill(new(bounds.Left, bounds.Top, bounds.Width, 2), highlight);
        Fill(new(bounds.Left, bounds.Bottom - 2, bounds.Width, 2), highlight);
        Fill(new(bounds.Left, bounds.Top, 2, bounds.Height), highlight);
        Fill(new(bounds.Right - 2, bounds.Top, 2, bounds.Height), highlight);
    }

    private void DrawInspector(CardDefinition card)
    {
        Box(InspectorPanel, new Color(19, 34, 43), new Color(73, 110, 120));
        Text("CARD DETAILS", 631, 94, 15, Teal);
        Button(InspectorClose, "Close", false, quiet: true);
        var preview = CardBounds(628, 121, 140);
        DrawCard(preview, card, true, enlarged: true);

        const int left = 789;
        Text(card.Name, left, 107, 30, Ivory);
        var type = card.Subtype is { Length: > 0 } subtype ? $"{card.Type} / {subtype}" : card.Type.ToString();
        Text($"{type}   |   {card.Cost} energy", left, 144, 18, Teal);
        var stats = card.Type == CardType.Unit ? $"{card.Power} Power / {card.Health} Health" : "";
        if (card.Aspect is { Length: > 0 } aspect)
        {
            stats = stats.Length > 0 ? $"{stats}   |   Aspect: {aspect}" : $"Aspect: {aspect}";
        }
        Text(stats, left, 166, 17, Muted);

        if (inspectorRows.Length == 0)
        {
            Text("No keyword or ability text.", left, 205, 18, Muted);
        }
        for (var i = 0; i < InspectorVisibleRows && i + inspectorScroll < inspectorRows.Length; i++)
        {
            Text(inspectorRows[i + inspectorScroll], left, 190 + i * 20, 18, Ivory);
        }
        if (inspectorRows.Length > InspectorVisibleRows)
        {
            Text("Scroll for more", left, 313, 14, Muted);
        }
    }

    private void DrawActions()
    {
        if (duel.Outcome != DuelOutcome.Ongoing)
        {
            Button(AtBottom(RematchButton), "Play again", true);
            return;
        }

        if (duel.Phase == DuelPhase.Mulligan)
        {
            Button(AtBottom(NextButton), mulligan.Count == 0 ? "Keep hand" : $"Replace {mulligan.Count} cards", true);
            return;
        }

        if (duel.PendingAttack is not null)
        {
            if (duel.ActivePlayer == Opponent)
            {
                Button(AtBottom(NextButton), "Take the hit", true);
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
                Button(AtBottom(SparkButton), "Use Spark", own.HasSpark);
                Button(AtBottom(NextButton), "Go to battle", true);
                break;
            case DuelPhase.Battle:
                Button(AtBottom(NextButton), "End battle", true);
                break;
            case DuelPhase.End:
                Button(AtBottom(DiscardButton), "Discard", selectedHand is not null && own.HandCount > Duel.HandLimit);
                Button(AtBottom(NextButton), "End turn", own.HandCount <= Duel.HandLimit);
                break;
        }
        if (selectedAttacker is not null || selectedHand is not null)
        {
            Button(AtBottom(CancelButton), "Cancel selection", true);
        }
    }

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
        Box(bounds, fill, selected ? Gold : ready ? Teal : Border);
        var inset = Math.Max(4, bounds.Width / 12);
        var nameFont = Math.Clamp(bounds.Width / 5, 10, enlarged ? 29 : 18);
        var detailFont = Math.Clamp(bounds.Width / 7, 9, enlarged ? 20 : 14);
        var interior = bounds.Width - 2 * inset;
        Fill(new(bounds.X + inset, bounds.Y + 6, interior, 2), selected ? Gold : accent);
        Text($"{card.Type.ToString().ToUpperInvariant()} {card.Cost}", bounds.X + inset, bounds.Y + 11,
            detailFont, accent);
        var nameRows = Wrap(card.Name, Math.Max(5, interior / TextAdvance(nameFont)), 2);
        for (var i = 0; i < nameRows.Length; i++)
        {
            Text(nameRows[i], bounds.X + inset, bounds.Y + 13 + detailFont + i * nameFont, nameFont, Ivory);
        }

        var description = card.Keywords != CardKeyword.None ? card.Keywords.ToString()
            : card.Abilities.Count > 0 ? Describe(card.Abilities[0]) : "";
        var descriptionRows = Wrap(description, Math.Max(5, interior / TextAdvance(detailFont)), 2);
        for (var i = 0; i < descriptionRows.Length; i++)
        {
            var y = bounds.Y + 15 + detailFont + nameFont * 2 + i * detailFont;
            if (y + detailFont <= bounds.Bottom - detailFont - 7)
            {
                Text(descriptionRows[i], bounds.X + inset, y, detailFont, Muted);
            }
        }

        if (card.Type == CardType.Unit)
        {
            Text($"{unit?.Power ?? card.Power}/{card.Health - (unit?.Damage ?? 0)}",
                bounds.X + inset, bounds.Bottom - nameFont - 5, nameFont, Ivory);
        }
        if (ready || unit is { Exhausted: true })
        {
            var label = ready ? unit!.CanIntercept ? "BLOCK" : "ATTACK" : "USED";
            var labelSize = Math.Max(9, detailFont - 1);
            Text(label, bounds.Right - inset - TextAdvance(labelSize) * label.Length,
                bounds.Bottom - labelSize - 6, labelSize, ready ? Teal : Muted);
        }
    }

    private static string Describe(CardAbility ability) => ability.Action switch
    {
        AbilityAction.Damage => $"Deal {ability.Amount} damage",
        AbilityAction.HealLeader => $"Arrival: heal {ability.Amount}",
        AbilityAction.Draw => "Last Breath: draw",
        AbilityAction.Ready => "Ready a unit; draw",
        AbilityAction.IncreasePower => "Start: +1 Power",
        _ => "",
    };

    private static string[] Wrap(string value, int width, int lines)
    {
        if (value.Length == 0)
        {
            return [];
        }
        var parts = new List<string>();
        while (value.Length > width && parts.Count < lines - 1)
        {
            var gap = value.LastIndexOf(' ', width);
            var split = gap > 0 ? gap : width;
            parts.Add(value[..split]);
            value = value[split..].TrimStart();
        }
        parts.Add(value.Length > width ? string.Concat(value.AsSpan(0, width - 1), "~") : value);
        return [.. parts];
    }

    private void Button(Rectangle bounds, string label, bool enabled, bool quiet = false)
    {
        Box(bounds, enabled ? new Color(43, 83, 88) : Panel, enabled ? Teal : Border);
        CenterText(label, bounds.Center.X, bounds.Y + 8, 21, enabled || quiet ? Ivory : Muted);
    }

    private void DashedSlot(Rectangle bounds)
    {
        RoundedFill(bounds, new Color(14, 25, 31));
        var dash = new Color(72, 104, 112);
        const int step = 12;
        const int length = 6;
        const int inset = 6;
        for (var x = bounds.Left + inset; x < bounds.Right - inset; x += step)
        {
            var width = Math.Min(length, bounds.Right - inset - x);
            Fill(new(x, bounds.Top, width, 1), dash);
            Fill(new(x, bounds.Bottom - 1, width, 1), dash);
        }
        for (var y = bounds.Top + inset; y < bounds.Bottom - inset; y += step)
        {
            var height = Math.Min(length, bounds.Bottom - inset - y);
            Fill(new(bounds.Left, y, 1, height), dash);
            Fill(new(bounds.Right - 1, y, 1, height), dash);
        }
    }

    private void Box(Rectangle bounds, Color fill, Color edge)
    {
        RoundedFill(bounds, edge);
        bounds.Inflate(-1, -1);
        RoundedFill(bounds, fill);
    }

    private void RoundedFill(Rectangle bounds, Color color)
    {
        Fill(new(bounds.X + 4, bounds.Y, bounds.Width - 8, 1), color);
        Fill(new(bounds.X + 2, bounds.Y + 1, bounds.Width - 4, 1), color);
        Fill(new(bounds.X + 1, bounds.Y + 2, bounds.Width - 2, 1), color);
        Fill(new(bounds.X, bounds.Y + 3, bounds.Width, bounds.Height - 6), color);
        Fill(new(bounds.X + 1, bounds.Bottom - 3, bounds.Width - 2, 1), color);
        Fill(new(bounds.X + 2, bounds.Bottom - 2, bounds.Width - 4, 1), color);
        Fill(new(bounds.X + 4, bounds.Bottom - 1, bounds.Width - 8, 1), color);
    }

    private void Fill(Rectangle bounds, Color color) => batch.Draw(pixel, bounds, color);

    private void CenterText(string text, int x, int y, int height, Color color) =>
        Text(text, x - TextAdvance(height) * text.Length / 2, y, height, color);

    private static int TextAdvance(int height) => BitmapText.Advance(height);

    private void Text(string text, int x, int y, int height, Color color) =>
        BitmapText.Draw(batch, font, text, x, y, height, color);

    private sealed class HandDrag(int slot, Point start, Point offset)
    {
        public int Slot { get; } = slot;
        public Point Start { get; } = start;
        public Point Offset { get; } = offset;
        public Point? Position { get; set; }
        public bool Started { get; set; }
        public IReadOnlyList<CardPlay> Plays { get; set; } = [];
    }
}
