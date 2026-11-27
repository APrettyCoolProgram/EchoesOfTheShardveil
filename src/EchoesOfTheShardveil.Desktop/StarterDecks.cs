// 261127_code
// 261127_documentation

using EchoesOfTheShardveil.Core;

namespace EchoesOfTheShardveil.Desktop;

/// <summary>Loads the starter deck recipes and resolves their card IDs.</summary>
internal static class StarterDecks
{
    private static readonly Lazy<IReadOnlyDictionary<string, CardDefinition>> Alpha =
        new(() => CardSet.Load("Alpha"));
    private static readonly Lazy<LoadedDeck> Player = new(() => Load("AlphaPlayer.json"));
    private static readonly Lazy<LoadedDeck> Opponent = new(() => Load("AlphaOpponent.json"));

    /// <summary>Creates a copy of the requested starter deck.</summary>
    /// <param name="secondDeck">Whether to use the opponent's deck recipe.</param>
    /// <returns>The cards in recipe order, including copies.</returns>
    public static IReadOnlyList<CardDefinition> Create(bool secondDeck = false) =>
        (CardDefinition[])(secondDeck ? Opponent.Value : Player.Value).Cards.Clone();

    /// <summary>Gets the playable card pool from the set named in the player recipe.</summary>
    public static IEnumerable<CardDefinition> PlayerPool => Player.Value.Pool.Values.Where(Duel.IsPlayableCard);

    /// <summary>Looks up an Alpha card used by the tutorial.</summary>
    /// <param name="id">The card's stable ID.</param>
    /// <returns>The loaded card definition.</returns>
    public static CardDefinition GetCard(string id) => Alpha.Value.TryGetValue(id, out var card)
        ? card
        : throw new InvalidDataException($"Alpha.cards is missing the required card '{id}'.");

    /// <summary>Reads a recipe and verifies its references and copy counts.</summary>
    /// <param name="file">The filename in the Deck folder.</param>
    /// <returns>The expanded deck and the set it was built from.</returns>
    private static LoadedDeck Load(string file)
    {
        var path = AppDataJson.PathFor("Deck", file);
        var recipe = AppDataJson.Load<DeckRecipe>(path);
        if (string.IsNullOrWhiteSpace(recipe.Set) || recipe.Set.IndexOfAny(['/', '\\']) >= 0 ||
            recipe.Set is "." or ".." || recipe.Cards is null)
        {
            throw new InvalidDataException($"'{path}' must specify a set name and card list.");
        }

        var pool = recipe.Set == "Alpha" ? Alpha.Value : CardSet.Load(recipe.Set);
        var cards = new List<CardDefinition>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in recipe.Cards)
        {
            if (entry is null || string.IsNullOrWhiteSpace(entry.Id) || !pool.TryGetValue(entry.Id, out var card) ||
                !Duel.IsPlayableCard(card) || !seen.Add(entry.Id) || entry.Copies < 1 || entry.Copies > MatchFormats.Alpha.MaxCopiesPerName)
            {
                throw new InvalidDataException($"'{path}' has an invalid deck entry for '{entry?.Id}'.");
            }
            cards.AddRange(Enumerable.Repeat(card, entry.Copies));
        }
        if (cards.Count != MatchFormats.Alpha.DeckSize)
        {
            throw new InvalidDataException($"'{path}' must contain exactly {MatchFormats.Alpha.DeckSize} cards.");
        }
        if (cards.GroupBy(card => card.Name, StringComparer.OrdinalIgnoreCase)
            .Any(group => group.Count() > MatchFormats.Alpha.MaxCopiesPerName))
        {
            throw new InvalidDataException($"'{path}' exceeds the copy limit for a card name.");
        }
        return new LoadedDeck(cards.ToArray(), pool);
    }

    /// <summary>A starter deck and the set its cards came from.</summary>
    /// <param name="Cards">The expanded deck.</param>
    /// <param name="Pool">All cards in the set, keyed by ID.</param>
    private sealed record LoadedDeck(CardDefinition[] Cards, IReadOnlyDictionary<string, CardDefinition> Pool);

    /// <summary>The set and card entries in a starter deck recipe.</summary>
    /// <param name="Set">The name of the card set.</param>
    /// <param name="Cards">The card IDs and copy counts.</param>
    private sealed record DeckRecipe(string Set, DeckEntry[] Cards);

    /// <summary>The copies of one card in a deck recipe.</summary>
    /// <param name="Id">The card's stable ID within the set.</param>
    /// <param name="Copies">The number of copies in the deck.</param>
    private sealed record DeckEntry(string Id, int Copies);
}
