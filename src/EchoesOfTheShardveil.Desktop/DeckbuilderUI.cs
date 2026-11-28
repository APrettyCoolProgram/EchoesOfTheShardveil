// 261127_code
// 261127_documentation

using EchoesOfTheShardveil.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace EchoesOfTheShardveil.Desktop;

/// <summary>Edits the player's single-player deck and retains the draft for the current session.</summary>
internal sealed class DeckbuilderUI : IScreen
{
    private const int Width = 1120;
    private const int Height = 760;
    private const int RowHeight = 35;
    private const int VisibleRows = 14;
    private const int VisibleDetailLines = 8;
    private const int HighestEnergyBucket = 6;
    private static readonly Rectangle Back = new(28, 28, 140, 42);
    private static readonly Rectangle SearchField = new(480, 28, 286, 42);
    private static readonly Rectangle ClearSearch = new(728, 34, 32, 30);
    private static readonly Rectangle TypeFilter = new(782, 28, 144, 42);
    private static readonly Rectangle Collection = new(28, 94, 668, 574);
    private static readonly Rectangle SetFilter = new(186, 107, 164, 29);
    private static readonly Rectangle InDeckFilter = new(360, 107, 112, 29);
    private static readonly Rectangle SortCost = new(44, 140, 53, 24);
    private static readonly Rectangle SortName = new(105, 140, 65, 24);
    private static readonly Rectangle PreviousCards = new(600, 107, 36, 29);
    private static readonly Rectangle NextCards = new(644, 107, 36, 29);
    private static readonly Rectangle Details = new(714, 94, 378, 408);
    private static readonly Rectangle Deck = new(714, 520, 378, 148);
    private static readonly Rectangle Revert = new(500, 688, 132, 44);
    private static readonly Rectangle Restore = new(642, 688, 150, 44);
    private static readonly Rectangle ApplyDeck = new(802, 688, 146, 44);
    private static readonly Rectangle PlayDraft = new(958, 688, 134, 44);
    private static readonly Color Background = new(12, 17, 22);
    private static readonly Color Panel = new(23, 34, 42);
    private static readonly Color Border = new(59, 80, 87);
    private static readonly Color Muted = new(170, 191, 194);
    private static readonly Color Gold = new(226, 190, 117);
    private static readonly Color Teal = new(38, 81, 81);

    private readonly GameServices services;
    private readonly SavedDeck savedDeck;
    private readonly WindowControls controls;
    private readonly InputTracker input = new();
    private readonly IReadOnlyList<CardDefinition> starter = StarterDecks.Create();
    private readonly (string Set, CardDefinition Card)[] cards;
    private readonly int[] copies;
    private readonly string[] setNames;
    private readonly CardType[] cardTypes;
    private int[] filteredCards = [];
    private bool inDeckOnly;
    private bool sortByName;
    private bool searchFocused;
    private string search = "";
    private int setFilterIndex = -1;
    private int typeFilterIndex = -1;
    private int[] activeCopies;
    private int selected = -1;
    private string? artworkPath;
    private bool loadArtwork;
    private Texture2D? artworkTexture;
    private int firstVisible;
    private Point? pointer;
    private List<(string Text, Color Color)> detailLines = [];
    private int detailScroll;
    private string status = "";

    /// <summary>Builds an editable list from the player starter recipe's sets.</summary>
    /// <param name="services">Shared graphics and audio resources.</param>
    /// <param name="savedDeck">The active single-player deck and its save file.</param>
    public DeckbuilderUI(GameServices services, SavedDeck savedDeck)
    {
        this.services = services;
        this.savedDeck = savedDeck;
        controls = new WindowControls(services.Audio, isBoard: false);
        cards = StarterDecks.PlayerPool.OrderBy(entry => entry.Card.Cost).ThenBy(entry => entry.Card.Name)
            .ThenBy(entry => entry.Set, StringComparer.Ordinal).ToArray();
        copies = new int[cards.Length];
        setNames = cards.Select(entry => entry.Set).Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal).ToArray();
        cardTypes = cards.Select(entry => entry.Card.Type).Distinct().Order().ToArray();
        SetDraft(savedDeck.ActiveDeck);
        if (savedDeck.UsedStarterFallback)
        {
            status = "Saved deck unavailable. Using starter deck.";
        }
        activeCopies = (int[])copies.Clone();
    }

    /// <inheritdoc />
    public string Title => "Echoes of the Shardveil - Deckbuilder";

    private int MaximumOffset => Math.Max(0, filteredCards.Length - VisibleRows);
    private int VisibleEnd => Math.Min(firstVisible + VisibleRows, filteredCards.Length);
    private int CardCount => copies.Sum();
    private bool HasDraftChanges => !copies.SequenceEqual(activeCopies);
    private bool HasCompleteDeck => CardCount == MatchFormats.Active.DeckSize;
    private bool CanApply => HasCompleteDeck &&
        (savedDeck.UsedStarterFallback || HasDraftChanges);

    /// <summary>Gets a snapshot for a playtest without applying or saving the draft.</summary>
    /// <returns>The complete draft, or null if it still needs edits.</returns>
    public IReadOnlyList<CardDefinition>? CreatePlaytestDeck() => HasCompleteDeck
        ? cards.SelectMany((entry, index) => Enumerable.Repeat(entry.Card, copies[index])).ToArray()
        : null;

    /// <summary>Ignores the click that opened the editor from the menu.</summary>
    public void Activate()
    {
        input.Reset();
        pointer = null;
        searchFocused = false;
    }

    /// <summary>Edits the card-name filter when its field has focus.</summary>
    /// <param name="character">The character received from the game window.</param>
    public void EnterSearchText(char character)
    {
        if (!searchFocused || controls.IsConfirming)
        {
            return;
        }

        switch (character)
        {
            case '\b' when search.Length > 0:
                SetSearch(search[..^1]);
                break;
            case '\r' or '\n' or '\t':
                searchFocused = false;
                break;
            default:
                if (!char.IsControl(character) && search.Length < 64)
                {
                    SetSearch(search + character);
                }
                break;
        }
    }

    /// <summary>Moves the selection through the current filtered collection and keeps it visible.</summary>
    /// <param name="key">The navigation key.</param>
    public void NavigateCollection(Keys key)
    {
        if (searchFocused || controls.IsConfirming || filteredCards.Length == 0)
        {
            return;
        }

        var row = Array.IndexOf(filteredCards, selected);
        var destination = key switch
        {
            Keys.Up => Math.Max(0, row - 1),
            Keys.Down => Math.Min(filteredCards.Length - 1, row + 1),
            Keys.PageUp => Math.Max(0, row - VisibleRows),
            Keys.PageDown => Math.Min(filteredCards.Length - 1, row + VisibleRows),
            Keys.Home => 0,
            Keys.End => filteredCards.Length - 1,
            _ => -1,
        };
        if (destination < 0)
        {
            return;
        }

        Select(filteredCards[destination]);
        if (destination < firstVisible)
        {
            firstVisible = destination;
        }
        else if (destination >= VisibleEnd)
        {
            firstVisible = destination - VisibleRows + 1;
        }
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
            if (controls.IsConfirming)
            {
                controls.ToggleExitPrompt();
            }
            else if (searchFocused)
            {
                searchFocused = false;
            }
            else
            {
                return ScreenTransition.ReturnToMenu;
            }
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
                if (HasCompleteDeck && PlayDraft.Contains(position))
                {
                    return ScreenTransition.PlayDraft;
                }
                Edit(position);
            }
        }

        if (!controls.IsConfirming && frame.ScrollDirection != 0 && pointer is { } scrollPosition)
        {
            if (Collection.Contains(scrollPosition))
            {
                ScrollCards(frame.ScrollDirection);
            }
            else if (Details.Contains(scrollPosition))
            {
                ScrollDetails(frame.ScrollDirection);
            }
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
        DrawSearch();
        if (cardTypes.Length > 1)
        {
            var type = typeFilterIndex < 0 ? "All types" : cardTypes[typeFilterIndex].ToString();
            Button(TypeFilter, type + " >", true, primary: typeFilterIndex >= 0);
        }
        DrawCollection();
        DrawDetails();
        DrawDeckSummary();
        Text(status.Length == 0 && HasDraftChanges ? "Unsaved changes" : status, 28, 699, 20, Muted);
        Button(Revert, "Revert edits", HasDraftChanges);
        Button(Restore, "Restore starter", true);
        Button(ApplyDeck, "Use this deck", CanApply, primary: true);
        Button(PlayDraft, "Play draft", HasCompleteDeck);
        services.SpriteBatch.End();
        controls.Draw(services.SpriteBatch, services.Pixel, services.Font, viewport);
    }

    /// <inheritdoc />
    public void RequestClose() => controls.RequestExitConfirmation();

    /// <inheritdoc />
    public void Dispose() => artworkTexture?.Dispose();

    /// <summary>Applies clicks to the draft, the selected card or the active deck.</summary>
    /// <param name="position">The pointer in design coordinates.</param>
    private void Edit(Point position)
    {
        searchFocused = SearchField.Contains(position);
        if (searchFocused)
        {
            if (search.Length > 0 && ClearSearch.Contains(position))
            {
                SetSearch("");
            }
        }
        else if (Revert.Contains(position) && HasDraftChanges)
        {
            SetDraft(savedDeck.ActiveDeck);
            status = "Draft reverted to the applied deck.";
        }
        else if (Restore.Contains(position))
        {
            SetDraft(starter);
            status = "Starter deck restored to the draft.";
        }
        else if (ApplyDeck.Contains(position) && CanApply)
        {
            var entries = cards.Select((card, index) => (card.Set, card.Card, Copies: copies[index]))
                .Where(entry => entry.Copies > 0);
            if (savedDeck.TryApply(entries))
            {
                activeCopies = (int[])copies.Clone();
                status = "Deck saved for Single player.";
            }
            else
            {
                status = "Could not save deck. Your draft is still here.";
            }
        }
        else if (setNames.Length > 1 && SetFilter.Contains(position))
        {
            CycleSetFilter();
        }
        else if (cardTypes.Length > 1 && TypeFilter.Contains(position))
        {
            typeFilterIndex = typeFilterIndex == cardTypes.Length - 1 ? -1 : typeFilterIndex + 1;
            firstVisible = 0;
            RefreshCollection();
        }
        else if (InDeckFilter.Contains(position))
        {
            inDeckOnly = !inDeckOnly;
            firstVisible = 0;
            RefreshCollection();
        }
        else if (SortCost.Contains(position) || SortName.Contains(position))
        {
            sortByName = SortName.Contains(position);
            firstVisible = 0;
            RefreshCollection();
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
            for (var visibleIndex = firstVisible; visibleIndex < VisibleEnd; visibleIndex++)
            {
                var index = filteredCards[visibleIndex];
                var row = Row(visibleIndex - firstVisible);
                if (!row.Contains(position))
                {
                    continue;
                }

                Select(index);
                if (RemoveButton(row).Contains(position) && copies[index] > 0)
                {
                    copies[index]--;
                    status = "";
                    if (inDeckOnly && copies[index] == 0)
                    {
                        RefreshCollection();
                    }
                }
                else if (AddButton(row).Contains(position) && CanAdd(index, CardCount))
                {
                    copies[index]++;
                    status = "";
                }
                break;
            }
        }
    }

    /// <summary>Cycles through the available sets and the unfiltered collection.</summary>
    private void CycleSetFilter()
    {
        setFilterIndex = setFilterIndex == setNames.Length - 1 ? -1 : setFilterIndex + 1;
        firstVisible = 0;
        RefreshCollection();
    }

    /// <summary>Updates the search text and returns to the first matching row.</summary>
    /// <param name="text">The new search text.</param>
    private void SetSearch(string text)
    {
        search = text;
        firstVisible = 0;
        RefreshCollection();
    }

    /// <summary>Applies sorting and filters and keeps the visible rows and selected card in range.</summary>
    private void RefreshCollection()
    {
        var query = search.Trim();
        var matching = Enumerable.Range(0, cards.Length)
            .Where(index => (setFilterIndex < 0 || cards[index].Set == setNames[setFilterIndex]) &&
                (typeFilterIndex < 0 || cards[index].Card.Type == cardTypes[typeFilterIndex]) &&
                (!inDeckOnly || copies[index] > 0) &&
                cards[index].Card.Name.Contains(query, StringComparison.OrdinalIgnoreCase));
        if (sortByName)
        {
            matching = matching.OrderBy(index => cards[index].Card.Name, StringComparer.CurrentCultureIgnoreCase);
        }
        filteredCards = matching.ToArray();
        firstVisible = Math.Min(firstVisible, MaximumOffset);
        if (filteredCards.Length == 0)
        {
            selected = -1;
            detailLines.Clear();
            detailScroll = 0;
            SetArtwork(null);
        }
        else if (!filteredCards.Contains(selected))
        {
            Select(filteredCards[firstVisible]);
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
        copies.Where((_, other) => StringComparer.OrdinalIgnoreCase.Equals(cards[index].Card.Name, cards[other].Card.Name))
            .Sum() < MatchFormats.Active.MaxCopiesPerName;

    /// <summary>Replaces the draft with the given deck and refreshes the collection.</summary>
    /// <param name="deck">The cards to put into the draft.</param>
    private void SetDraft(IReadOnlyList<CardDefinition> deck)
    {
        for (var index = 0; index < cards.Length; index++)
        {
            copies[index] = deck.Count(card => ReferenceEquals(card, cards[index].Card));
        }
        RefreshCollection();
    }

    /// <summary>Shows a card and prepares its rules and flavor text for the detail panel.</summary>
    /// <param name="index">The selected card's index.</param>
    private void Select(int index)
    {
        if (selected == index)
        {
            return;
        }

        var (set, card) = cards[index];
        var presentation = CardSet.Presentation(set, card.Id);
        selected = index;
        detailScroll = 0;
        detailLines = CardRulesText.Describe(card).SelectMany(rule => BitmapText.Wrap(rule, 37))
            .Select(rule => (rule, UiColors.Ivory)).ToList();
        if (!string.IsNullOrWhiteSpace(presentation?.FlavorText))
        {
            if (detailLines.Count > 0)
            {
                detailLines.Add(("", Muted));
            }
            foreach (var paragraph in presentation.FlavorText.Split('\n', StringSplitOptions.TrimEntries))
            {
                var lines = paragraph.Length == 0 ? [""] : BitmapText.Wrap(paragraph, 37);
                detailLines.AddRange(lines.Select(line => (line, Muted)));
            }
        }
        SetArtwork(presentation?.Artwork);
    }

    /// <summary>Scrolls rules and flavor text without moving the card header.</summary>
    /// <param name="direction">The signed scroll direction.</param>
    private void ScrollDetails(int direction) =>
        detailScroll = Math.Clamp(detailScroll + direction, 0, Math.Max(0, detailLines.Count - VisibleDetailLines));

    /// <summary>Replaces the active preview when the selected card's artwork changes.</summary>
    /// <param name="path">The image path, or null if there is no artwork.</param>
    private void SetArtwork(string? path)
    {
        if (artworkPath == path)
        {
            return;
        }

        artworkTexture?.Dispose();
        artworkTexture = null;
        artworkPath = path;
        loadArtwork = path is not null;
    }

    /// <summary>Loads optional artwork once per selection, falling back to text if it is unavailable.</summary>
    private void LoadArtwork()
    {
        if (!loadArtwork || artworkPath is null)
        {
            return;
        }

        loadArtwork = false;
        try
        {
            using var stream = File.OpenRead(artworkPath);
            artworkTexture = Texture2D.FromStream(services.Device, stream, DefaultColorProcessors.PremultiplyAlpha);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or
            InvalidOperationException or ArgumentException or NotSupportedException)
        {
            // Missing or unreadable artwork leaves the card details available.
        }
    }

    /// <summary>Draws deck size and the energy cost of every copy in the draft.</summary>
    private void DrawDeckSummary()
    {
        Box(Deck, Panel);
        var count = CardCount;
        var format = MatchFormats.Active;
        Text("Your deck", 734, 534, 29, UiColors.Ivory);
        Text($"{count} / {format.DeckSize} cards", 734, 574, 22, Gold);
        const int meterWidth = 140;
        Fill(new Rectangle(734, 611, meterWidth, 5), Border);
        Fill(new Rectangle(734, 611, (int)((long)meterWidth * count / format.DeckSize), 5), Gold);
        Text($"{format.MaxCopiesPerName} copies per name", 734, 631, 16, Muted);

        Text("Energy curve", 895, 534, 19, UiColors.Ivory);
        const int baseline = 626;
        Fill(new Rectangle(894, baseline, 175, 1), Border);
        var buckets = CountEnergyCosts();
        var peak = Math.Max(1, buckets.Max());
        for (var cost = 0; cost < buckets.Length; cost++)
        {
            var center = 906 + cost * 25;
            var copiesAtCost = buckets[cost];
            if (copiesAtCost > 0)
            {
                var barHeight = Math.Max(2, (int)(48L * copiesAtCost / peak));
                Fill(new Rectangle(center - 7, baseline - barHeight, 14, barHeight), Teal);
                BitmapText.DrawCentered(services.SpriteBatch, services.Font, $"{copiesAtCost}",
                    center, baseline - barHeight - 16, 14, UiColors.Ivory);
            }
            var label = cost == HighestEnergyBucket ? $"{cost}+" : $"{cost}";
            BitmapText.DrawCentered(services.SpriteBatch, services.Font, label, center, baseline + 5, 15, Muted);
        }
    }

    /// <summary>Counts draft copies at each energy cost, grouping higher costs into the final bucket.</summary>
    /// <returns>The counts in ascending cost order.</returns>
    private int[] CountEnergyCosts()
    {
        var buckets = new int[HighestEnergyBucket + 1];
        for (var index = 0; index < cards.Length; index++)
        {
            buckets[Math.Min(cards[index].Card.Cost, HighestEnergyBucket)] += copies[index];
        }
        return buckets;
    }

    /// <summary>Draws the name filter, its text cursor and its clear button.</summary>
    private void DrawSearch()
    {
        const int textHeight = 20;
        const int textLimit = 22;
        const int left = 492;
        const int top = 39;
        Box(SearchField, searchFocused ? Teal : Panel);
        var visibleText = search.Length <= textLimit ? search : search[^textLimit..];
        Text(search.Length == 0 ? "Search card names" : visibleText, left, top, textHeight,
            search.Length == 0 ? Muted : UiColors.Ivory);
        if (searchFocused && search.Length > 0)
        {
            Fill(new Rectangle(left + visibleText.Length * BitmapText.Advance(textHeight), top + 1, 1, 18), Gold);
        }
        if (search.Length > 0)
        {
            Button(ClearSearch, "X", true);
        }
    }

    /// <summary>Draws the card list and the copy controls for each card.</summary>
    private void DrawCollection()
    {
        Box(Collection, Panel);
        Text("Card pool", 48, 106, 29, UiColors.Ivory);
        Button(SortCost, "Cost", true, primary: !sortByName);
        Button(SortName, "Name", true, primary: sortByName);
        Text("Type", 391, 143, 17, Muted);
        Text("Set", 454, 143, 17, Muted);
        Text("Copies", 575, 143, 17, Muted);
        if (setNames.Length > 1)
        {
            var label = setFilterIndex < 0 ? "All sets" : BitmapText.Wrap(setNames[setFilterIndex], 12, 1)[0];
            Button(SetFilter, label + " >", true);
        }
        Button(InDeckFilter, "In deck", true, primary: inDeckOnly);
        if (filteredCards.Length == 0)
        {
            Text("No cards match these filters.", 48, 176, 20, Muted);
        }
        if (MaximumOffset > 0)
        {
            Text($"{firstVisible + 1}-{VisibleEnd} of {filteredCards.Length}", 483, 112, 16, Muted);
            Button(PreviousCards, "<", firstVisible > 0);
            Button(NextCards, ">", firstVisible < MaximumOffset);
        }

        var count = CardCount;
        for (var visibleIndex = firstVisible; visibleIndex < VisibleEnd; visibleIndex++)
        {
            var index = filteredCards[visibleIndex];
            var row = Row(visibleIndex - firstVisible);
            var (set, card) = cards[index];
            Fill(row, index == selected ? Teal : new Color(29, 43, 51));
            if (index == selected)
            {
                Fill(new Rectangle(row.X, row.Y, 3, row.Height), Gold);
            }

            Text($"{card.Cost}", 60, row.Top + 5, 22, Gold);
            Text(BitmapText.Wrap(card.Name, 24, 1)[0], 111, row.Top + 5, 22, UiColors.Ivory);
            Text(card.Type.ToString(), 391, row.Top + 6, 20, Muted);
            Text(BitmapText.Wrap(set, 9, 1)[0], 454, row.Top + 7, 17, Muted);
            Text($"{copies[index]}", 595, row.Top + 5, 22, UiColors.Ivory);
            Button(RemoveButton(row), "-", copies[index] > 0);
            Button(AddButton(row), "+", CanAdd(index, count));
        }
    }

    /// <summary>Draws the selected card's stats and rules.</summary>
    private void DrawDetails()
    {
        Box(Details, Panel);
        if (selected < 0)
        {
            return;
        }

        var (set, card) = cards[selected];
        LoadArtwork();
        if (artworkTexture is { } image)
        {
            var bounds = ScreenLayout.Fit(new Viewport(0, 0, 143, 113), image.Width, image.Height);
            bounds.Offset(929, 146);
            services.SpriteBatch.Draw(image, bounds, Color.White);
        }

        Text(BitmapText.Wrap(card.Name, 23, 1)[0], 734, 113, 29, UiColors.Ivory);
        var type = string.IsNullOrEmpty(card.Subtype) ? card.Type.ToString() : $"{card.Type} / {card.Subtype}";
        Text(BitmapText.Wrap(type, artworkTexture is null ? 33 : 18, 1)[0], 734, 153, 21, Muted);
        Text(BitmapText.Wrap($"Set: {set}", artworkTexture is null ? 37 : 20, 1)[0], 734, 173, 18, Muted);
        Text($"{card.Cost} energy", 734, 193, 22, Gold);
        if (card.Type == CardType.Unit)
        {
            Text($"{card.Power} Power   {card.Health} Health", 734, 227, 22, UiColors.Ivory);
        }
        Fill(new Rectangle(734, 269, 338, 1), Border);
        var end = Math.Min(detailScroll + VisibleDetailLines, detailLines.Count);
        for (var index = detailScroll; index < end; index++)
        {
            var line = detailLines[index];
            Text(line.Text, 734, 286 + (index - detailScroll) * 25, 19, line.Color);
        }
        if (detailLines.Count > VisibleDetailLines)
        {
            Text($"Scroll  {detailScroll + 1}-{end} of {detailLines.Count}", 734, 481, 15, Muted);
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
