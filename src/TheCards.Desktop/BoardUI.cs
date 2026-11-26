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
    private readonly Texture2D heroHealth;
    private readonly Texture2D heroDropRing;
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

    public BoardUI(SpriteBatch batch, Texture2D pixel, Texture2D font,
        Texture2D heroPortrait, Texture2D heroHealth, Texture2D heroDropRing)
    {
        this.batch = batch;
        this.pixel = pixel;
        this.font = font;
        this.heroPortrait = heroPortrait;
        this.heroHealth = heroHealth;
        this.heroDropRing = heroDropRing;
        duel = NewDuel();
        own = duel.View(Human, Human);
        enemy = duel.View(Opponent, Human);
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
        else if (OpponentLeaderBounds().Contains(position))
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

    private Rectangle OpponentLeaderBounds() => CardBounds(28, SideTop(Opponent), 80);
    private Rectangle HeroPortraitBounds() => new(24, SideTop(Human) - 2, HeroPortraitSize, HeroPortraitSize);
    private Rectangle HeroHealthBounds()
    {
        var portrait = HeroPortraitBounds();
        return new(portrait.Center.X - HeroHealthSize / 2, portrait.Bottom - HeroHealthSize / 2,
            HeroHealthSize, HeroHealthSize);
    }

    private bool IsOverHeroPortrait(Point position)
    {
        var delta = position - HeroPortraitBounds().Center;
        return delta.X * delta.X + delta.Y * delta.Y <= HeroPortraitSize * HeroPortraitSize / 4;
    }
    private Rectangle OpponentWellBounds() => CardBounds(117, SideTop(Opponent), 55);
    private Rectangle UnitBounds(int player, int slot) => CardBounds(265 + slot * 150, SideTop(player));
    private Rectangle RelicBounds(int player, int slot) => CardBounds(1008 + slot * 131, SideTop(player));

    private static Rectangle CardBounds(int x, int y, int width = 95) =>
        new(x, y, width, width / 5 * 7);

    private Rectangle HandBounds(int slot, int count)
    {
        const int spacing = 8;
        var width = Math.Min(95, (1224 - spacing * (count - 1)) / Math.Max(1, count));
        width = Math.Max(5, width / 5 * 5);
        var total = width * count + spacing * (count - 1);
        return CardBounds((Width - total) / 2 + (width + spacing) * slot, layoutHeight - width / 5 * 7 / 2, width);
    }

    public void Draw(BoardViewport view, RasterizerState rasterizer)
    {
        SetLayout(view);
        batch.Begin(samplerState: SamplerState.LinearClamp, rasterizerState: rasterizer,
            transformMatrix: view.Transform);
        Fill(AcrossWindow(0, layoutHeight), BackgroundColor);
        Fill(AcrossWindow(layoutHeight - 164, 164), new Color(16, 28, 35));
        Fill(AcrossWindow(layoutHeight - 165, 1), Border);
        RoundedFill(new(230, SideTop(Opponent) - 19, 1022, 166), new Color(19, 32, 39));
        RoundedFill(new(230, SideTop(Human) - 19, 1022, 166), new Color(19, 32, 39));
        DrawSide(Opponent, enemy);
        DrawSide(Human, own);
        DrawCenter();
        DrawHand();
        DrawActions();
        DrawDrag();
        if (tutorial.IsOpen)
        {
            DrawTutorial();
        }
        else if (inspectedCard is { } card)
        {
            DrawInspector(card);
        }
        batch.End();
    }

    private void DrawSide(int player, PlayerView view)
    {
        var top = SideTop(player);
        var accent = player == Human ? Teal : Rose;
        var attackTarget = player == Opponent && selectedAttacker is not null;
        var health = view.Health.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var shards = view.Shards.ToString(System.Globalization.CultureInfo.InvariantCulture);
        int statsX;
        if (player == Human)
        {
            var portrait = HeroPortraitBounds();
            batch.Draw(heroPortrait, portrait, Color.White);
            if (own.CanFeedWell && handDrag is { Started: true, Position: { } pointer })
            {
                var dropRing = portrait;
                dropRing.Inflate(3, 3);
                batch.Draw(heroDropRing, dropRing, IsOverHeroPortrait(pointer) ? Gold : Teal);
            }

            var wellCount = new Rectangle(portrait.Center.X - 17, portrait.Y + 5, 34, 25);
            RoundedFill(wellCount, BackgroundColor * 0.90f);
            CenterText(shards, wellCount.Center.X, wellCount.Y + 2, 23, Teal);

            var healthCount = HeroHealthBounds();
            var cellSize = heroHealth.Width / HealthAtlasColumns;
            var healthTile = new Rectangle(view.Health % HealthAtlasColumns * cellSize,
                view.Health / HealthAtlasColumns * cellSize, cellSize, cellSize);
            batch.Draw(heroHealth, healthCount, healthTile, Color.White);
            statsX = portrait.Right + 4;
            Text($"{view.ReadyShards} READY", statsX, top + 20, 12, Teal);
        }
        else
        {
            var leader = OpponentLeaderBounds();
            Box(leader, Panel, attackTarget ? Gold : Border);
            CenterText("LEADER", leader.Center.X, leader.Y + 12, 15, accent);
            CenterText(health, leader.Center.X, leader.Y + 35, 37, Ivory);
            CenterText($"/ {Duel.StartingHealth}", leader.Center.X, leader.Y + 78, 15, Muted);

            var well = OpponentWellBounds();
            DashedSlot(well);
            CenterText("WELL", well.Center.X, well.Y + 8, 13, accent);
            CenterText(shards, well.Center.X, well.Y + 22, 27, Ivory);
            CenterText($"{view.ReadyShards} READY", well.Center.X, well.Y + 54, 11, Muted);
            statsX = well.Right + 10;
            Text("OPPONENT", statsX, top + 3, 16, accent);
        }

        var statSize = player == Human ? 12 : 14;
        Text($"DECK {view.DeckCount}", statsX, top + 36, statSize, Muted);
        Text($"HAND {view.HandCount}", statsX, top + 52, statSize, Muted);
        Text($"DISCARD {view.Discard.Count}", statsX, top + 68, statSize, Muted);
        if (view.HasSpark)
        {
            Text("SPARK READY", statsX, top + 86, statSize, Gold);
        }
        if (view.TemporaryEnergy > 0)
        {
            Text($"+{view.TemporaryEnergy} ENERGY", statsX, top + 104, statSize, Gold);
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
        Fill(new(left + 10, top + 92, width - 20, 16), Panel);
        CenterText("PROMPT", left + width / 2, top + 92, 14, Ivory);
        CenterText("ACTIONS", left + width / 2, top + 159, 14, Muted);
        CenterText("YOUR HAND", left + width / 2, top + 177, 14, Teal);
    }

    private void DrawCenter()
    {
        var bounds = new Rectangle(28, 271 + ExtraHeight / 2, 1224, 70);
        Box(bounds, new Color(24, 39, 44), Border);
        CenterText(Instruction(), bounds.Center.X, bounds.Y + 10, 22, Ivory);
        var detail = message;
        if (detail.Length == 0 && selectedHand is int selected)
        {
            detail = CardHint(own.Hand![selected]);
        }
        if (detail.Length > 0)
        {
            CenterText(detail, bounds.Center.X, bounds.Y + 42, 16, Muted);
        }
    }

    private string Instruction()
    {
        if (duel.Outcome != DuelOutcome.Ongoing)
        {
            return duel.Outcome switch
            {
                DuelOutcome.PlayerOneWins => "You win.",
                DuelOutcome.PlayerTwoWins => "Opponent wins.",
                _ => "The duel is a draw.",
            };
        }

        if (duel.PendingAttack is { } attack)
        {
            if (duel.ActivePlayer == Human)
            {
                return "Opponent is deciding whether to intercept.";
            }
            var target = attack.Target is { } unit ? own.Units[unit.Slot]!.Card.Name : "your Hero";
            return $"Attack on {target}. Choose an eligible unit or take the hit.";
        }
        if (duel.Phase != DuelPhase.Mulligan && duel.ActivePlayer == Opponent)
        {
            return "Opponent's turn";
        }
        return duel.Phase switch
        {
            DuelPhase.Mulligan => "Choose any opening cards to replace, then confirm.",
            DuelPhase.Main when handDrag is { Started: true, Plays.Count: 0 } && !own.CanFeedWell => "No legal destination for this card.",
            DuelPhase.Main when handDrag is { Started: true } => "Drop the card on a highlighted space.",
            DuelPhase.Main => "Drag a card to a slot to play it, or onto your portrait to feed the Well.",
            DuelPhase.Battle when selectedAttacker is not null => "Choose an enemy unit or click the opponent's Leader.",
            DuelPhase.Battle => "Choose a unit marked ATTACK, or end the battle.",
            DuelPhase.End when own.HandCount > Duel.HandLimit => "Select cards to discard until your hand has 10 cards.",
            _ => "End your turn when ready.",
        };
    }

    private void DrawHand()
    {
        Text("YOUR HAND", 28, layoutHeight - 163, 14, Muted);
        Text("Hover to reveal / Right-click for details", 28, layoutHeight - 143, 15, Muted);
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

    private static string CardHint(CardDefinition card)
    {
        var rule = card.Keywords switch
        {
            CardKeyword.Rush => "Can attack on the turn it enters play.",
            CardKeyword.Vigilant => "Stays ready after attacking.",
            CardKeyword.Sentinel => "Can intercept while exhausted.",
            CardKeyword.Elusive => "Only Elusive units can intercept its attacks.",
            CardKeyword.Pierce => "Excess combat damage hits the enemy Leader.",
            CardKeyword.Drain => "Damage dealt restores that much Health to your Hero.",
            _ => card.Id switch
            {
                "spark-bolt" => "Deal 3 damage to a unit.",
                "second-wind" => "Ready one of your units, then draw a card.",
                "beacon-of-tides" => "At the start of your turn, give a random friendly unit +1 Power.",
                _ => card.Abilities.Count > 0 ? Describe(card.Abilities[0]) : "",
            },
        };
        return rule.Length == 0 ? card.Name : $"{card.Name}: {rule}";
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
