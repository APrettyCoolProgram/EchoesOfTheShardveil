// 261127_code
// 261127_documentation

using EchoesOfTheShardveil.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EchoesOfTheShardveil.Desktop;

/// <summary>Edits the player's single-player deck and retains the draft for the current session.</summary>
internal sealed class DeckbuilderUI : IScreen
{
    private const int Width = 1120;
    private const int Height = 760;
    private const int RowHeight = 35;
    private const int VisibleRows = 14;
    private static readonly Rectangle Back = new(28, 28, 140, 42);
    private static readonly Rectangle Collection = new(28, 94, 668, 574);
    private static readonly Rectangle PreviousCards = new(600, 107, 36, 29);
    private static readonly Rectangle NextCards = new(644, 107, 36, 29);
    private static readonly Rectangle Details = new(714, 94, 378, 408);
    private static readonly Rectangle Deck = new(714, 520, 378, 148);
    private static readonly Rectangle Restore = new(714, 688, 172, 44);
    private static readonly Rectangle ApplyDeck = new(900, 688, 192, 44);
    private static readonly Color Background = new(12, 17, 22);
    private static readonly Color Panel = new(23, 34, 42);
    private static readonly Color Border = new(59, 80, 87);
    private static readonly Color Muted = new(170, 191, 194);
    private static readonly Color Gold = new(226, 190, 117);
    private static readonly Color Teal = new(38, 81, 81);

    private readonly GameServices services;
    private readonly WindowControls controls;
    private readonly InputTracker input = new();
    private readonly IReadOnlyList<CardDefinition> starter = StarterDecks.Create();
    private readonly CardDefinition[] cards;
    private readonly int[] copies;
    private int[] activeCopies;
    private int selected;
    private int firstVisible;
    private Point? pointer;
    private string[] rules = [];
    private string status = "";

    /// <summary>Builds an editable list from the player starter deck's set.</summary>
    /// <param name="services">Shared graphics and audio resources.</param>
    public DeckbuilderUI(GameServices services)
    {
        this.services = services;
        controls = new WindowControls(services.Audio, isBoard: false);
        cards = StarterDecks.PlayerPool.OrderBy(card => card.Cost).ThenBy(card => card.Name).ToArray();
        copies = new int[cards.Length];
        ActiveDeck = starter;
        RestoreStarter();
        activeCopies = (int[])copies.Clone();
        Select(0);
    }

    /// <inheritdoc />
    public string Title => "Echoes of the Shardveil - Deckbuilder";

    /// <summary>Gets the deck to use when starting the next duel.</summary>
    public IReadOnlyList<CardDefinition> ActiveDeck { get; private set; }

    private int MaximumOffset => Math.Max(0, cards.Length - VisibleRows);
    private int VisibleEnd => Math.Min(firstVisible + VisibleRows, cards.Length);
    private int CardCount => copies.Sum();
    private bool CanApply => CardCount == MatchFormats.Active.DeckSize && !copies.SequenceEqual(activeCopies);

    /// <summary>Ignores the click that opened the editor from the menu.</summary>
    public void Activate()
    {
        input.Reset();
        pointer = null;
    }

    /// <inheritdoc />
    public ScreenTransition Update(GameTime gameTime, Rectangle client, bool isActive)
    {
        var frame = input.Poll();
        var viewport = services.Device.Viewport;
        var fitted = ScreenLayout.Fit(viewport, Width, Height);
        pointer = isActive && ScreenLayout.TryMap(frame.Pointer, client, viewport, fitted, Width, Height, out var point)
            ? point : null;
        if (!isActive)
        {
            return ScreenTransition.None;
        }

        if (frame.EscapePressed)
        {
            if (!controls.IsConfirming)
            {
                return ScreenTransition.ReturnToMenu;
            }
            controls.ToggleExitPrompt();
        }
        else if (frame.LeftPressed)
        {
            var action = controls.Click(frame.Pointer, client, viewport);
            if (action == WindowControlResult.ConfirmedExit)
            {
                return ScreenTransition.ExitGame;
            }
            if (action == WindowControlResult.Ignored && pointer is { } position)
            {
                if (Back.Contains(position))
                {
                    return ScreenTransition.ReturnToMenu;
                }
                Edit(position);
            }
        }

        if (!controls.IsConfirming && pointer is { } scrollPosition && Collection.Contains(scrollPosition))
        {
            ScrollCards(frame.ScrollDirection);
        }

        return ScreenTransition.None;
    }

    /// <inheritdoc />
    public void Draw(GameTime gameTime)
    {
        var device = services.Device;
        device.Clear(Background);
        var viewport = device.Viewport;
        var fitted = ScreenLayout.Fit(viewport, Width, Height);
        if (fitted.Width <= 0 || fitted.Height <= 0)
        {
            return;
        }

        var transform = Matrix.CreateScale((float)fitted.Width / Width, (float)fitted.Height / Height, 1)
            * Matrix.CreateTranslation(fitted.X, fitted.Y, 0);
        services.SpriteBatch.Begin(samplerState: SamplerState.LinearClamp, transformMatrix: transform);
        Button(Back, "Main menu", true);
        Text("Deckbuilder", 192, 22, 43, UiColors.Ivory);
        DrawCollection();
        DrawDetails();
        Box(Deck, Panel);
        Text("Your deck", 734, 534, 29, UiColors.Ivory);
        Text($"{CardCount} / {MatchFormats.Active.DeckSize} cards", 734, 574, 25, Gold);
        Fill(new Rectangle(734, 615, 338, 5), Border);
        Fill(new Rectangle(734, 615, 338 * CardCount / MatchFormats.Active.DeckSize, 5), Gold);
        Text($"Up to {MatchFormats.Active.MaxCopiesPerName} copies per card", 734, 630, 19, Muted);
        Text(status, 28, 699, 20, Muted);
        Button(Restore, "Restore starter", true);
        Button(ApplyDeck, "Use this deck", CanApply, primary: true);
        services.SpriteBatch.End();
        controls.Draw(services.SpriteBatch, services.Pixel, services.Font, viewport);
    }

    /// <inheritdoc />
    public void RequestClose() => controls.RequestExitConfirmation();

    /// <inheritdoc />
    public void Dispose() { }

    /// <summary>Applies clicks to the draft, the selected card or the active deck.</summary>
    /// <param name="position">The pointer in design coordinates.</param>
    private void Edit(Point position)
    {
        if (Restore.Contains(position))
        {
            RestoreStarter();
            status = "Starter deck restored to the draft.";
        }
        else if (ApplyDeck.Contains(position) && CanApply)
        {
            ActiveDeck = cards.SelectMany((card, index) => Enumerable.Repeat(card, copies[index])).ToArray();
            activeCopies = (int[])copies.Clone();
            status = "Deck ready for Single player.";
        }
        else if (PreviousCards.Contains(position))
        {
            ScrollCards(-VisibleRows);
        }
        else if (NextCards.Contains(position))
        {
            ScrollCards(VisibleRows);
        }
        else
        {
            for (var index = firstVisible; index < VisibleEnd; index++)
            {
                var row = Row(index - firstVisible);
                if (!row.Contains(position))
                {
                    continue;
                }

                Select(index);
                if (RemoveButton(row).Contains(position) && copies[index] > 0)
                {
                    copies[index]--;
                    status = "";
                }
                if (AddButton(row).Contains(position) && CanAdd(index, CardCount))
                {
                    copies[index]++;
                    status = "";
                }
                break;
            }
        }
    }

    /// <summary>Moves the first visible collection row within the available range.</summary>
    /// <param name="rows">The signed number of rows to move.</param>
    private void ScrollCards(int rows) => firstVisible = Math.Clamp(firstVisible + rows, 0, MaximumOffset);

    /// <summary>Checks the deck size and the combined copy count across cards with the same name.</summary>
    /// <param name="index">The card to add.</param>
    /// <param name="count">The current draft size.</param>
    /// <returns>Whether another copy can be added.</returns>
    private bool CanAdd(int index, int count) =>
        count < MatchFormats.Active.DeckSize &&
        copies.Where((_, other) => StringComparer.OrdinalIgnoreCase.Equals(cards[index].Name, cards[other].Name))
            .Sum() < MatchFormats.Active.MaxCopiesPerName;

    /// <summary>Resets the draft to the original starter composition.</summary>
    private void RestoreStarter()
    {
        for (var index = 0; index < cards.Length; index++)
        {
            copies[index] = starter.Count(card => card.Id == cards[index].Id);
        }
    }

    /// <summary>Shows a card and prepares its rules for the detail panel.</summary>
    /// <param name="index">The selected card's index.</param>
    private void Select(int index)
    {
        selected = index;
        rules = CardRulesText.Describe(cards[index]).SelectMany(rule => BitmapText.Wrap(rule, 37)).ToArray();
    }

    /// <summary>Draws the card list and the copy controls for each card.</summary>
    private void DrawCollection()
    {
        Box(Collection, Panel);
        Text("Card pool", 48, 106, 29, UiColors.Ivory);
        Text("Cost", 49, 143, 17, Muted);
        Text("Name", 111, 143, 17, Muted);
        Text("Type", 423, 143, 17, Muted);
        Text("Copies", 575, 143, 17, Muted);
        if (MaximumOffset > 0)
        {
            Text($"{firstVisible + 1}-{VisibleEnd} of {cards.Length}", 439, 111, 18, Muted);
            Button(PreviousCards, "<", firstVisible > 0);
            Button(NextCards, ">", firstVisible < MaximumOffset);
        }

        var count = CardCount;
        for (var index = firstVisible; index < VisibleEnd; index++)
        {
            var row = Row(index - firstVisible);
            var card = cards[index];
            Fill(row, index == selected ? Teal : new Color(29, 43, 51));
            if (index == selected)
            {
                Fill(new Rectangle(row.X, row.Y, 3, row.Height), Gold);
            }

            Text($"{card.Cost}", 60, row.Top + 5, 22, Gold);
            Text(card.Name, 111, row.Top + 5, 22, UiColors.Ivory);
            Text(card.Type.ToString(), 423, row.Top + 6, 20, Muted);
            Text($"{copies[index]}", 595, row.Top + 5, 22, UiColors.Ivory);
            Button(RemoveButton(row), "-", copies[index] > 0);
            Button(AddButton(row), "+", CanAdd(index, count));
        }
    }

    /// <summary>Draws the selected card's stats and rules.</summary>
    private void DrawDetails()
    {
        var card = cards[selected];
        Box(Details, Panel);
        Text(card.Name, 734, 113, 29, UiColors.Ivory);
        var type = string.IsNullOrEmpty(card.Subtype) ? card.Type.ToString() : $"{card.Type} / {card.Subtype}";
        Text(type, 734, 153, 21, Muted);
        Text($"{card.Cost} energy", 734, 193, 22, Gold);
        if (card.Type == CardType.Unit)
        {
            Text($"{card.Power} Power   {card.Health} Health", 734, 227, 22, UiColors.Ivory);
        }
        Fill(new Rectangle(734, 269, 338, 1), Border);
        for (var index = 0; index < rules.Length; index++)
        {
            Text(rules[index], 734, 286 + index * 25, 19, UiColors.Ivory);
        }
    }

    /// <summary>Gets the design bounds of a card row.</summary>
    /// <param name="index">The card's index.</param>
    /// <returns>The row bounds.</returns>
    private static Rectangle Row(int index) => new(44, 167 + index * RowHeight, 636, RowHeight - 2);

    /// <summary>Gets the bounds of the button that removes one copy.</summary>
    /// <param name="row">The card row.</param>
    /// <returns>The remove button bounds.</returns>
    private static Rectangle RemoveButton(Rectangle row) => new(540, row.Y + 2, 32, 29);

    /// <summary>Gets the bounds of the button that adds one copy.</summary>
    /// <param name="row">The card row.</param>
    /// <returns>The add button bounds.</returns>
    private static Rectangle AddButton(Rectangle row) => new(632, row.Y + 2, 32, 29);

    /// <summary>Draws a button with hover and disabled states.</summary>
    /// <param name="bounds">The button bounds.</param>
    /// <param name="label">The button caption.</param>
    /// <param name="enabled">Whether the button can be used.</param>
    /// <param name="primary">Whether to accent the main action.</param>
    private void Button(Rectangle bounds, string label, bool enabled, bool primary = false)
    {
        var hovered = enabled && pointer is { } position && bounds.Contains(position) && !controls.IsConfirming;
        Box(bounds, hovered ? new Color(53, 99, 101) : primary && enabled ? Teal : Panel);
        const int textHeight = 20;
        BitmapText.DrawCentered(services.SpriteBatch, services.Font, label, bounds.Center.X,
            bounds.Center.Y - textHeight / 2, textHeight, enabled ? UiColors.Ivory : new Color(121, 141, 145));
    }

    /// <summary>Draws a panel with a one-pixel border.</summary>
    /// <param name="bounds">The outer bounds.</param>
    /// <param name="color">The interior color.</param>
    private void Box(Rectangle bounds, Color color)
    {
        Fill(bounds, Border);
        bounds.Inflate(-1, -1);
        Fill(bounds, color);
    }

    /// <summary>Draws a filled rectangle.</summary>
    /// <param name="bounds">The rectangle to fill.</param>
    /// <param name="color">The fill color.</param>
    private void Fill(Rectangle bounds, Color color) => services.SpriteBatch.Draw(services.Pixel, bounds, color);

    /// <summary>Draws a line in the shared bitmap font.</summary>
    /// <param name="label">The text to draw.</param>
    /// <param name="x">The left edge.</param>
    /// <param name="y">The top edge.</param>
    /// <param name="height">The text height.</param>
    /// <param name="color">The text color.</param>
    private void Text(string label, int x, int y, int height, Color color) =>
        BitmapText.Draw(services.SpriteBatch, services.Font, label, x, y, height, color);
}
