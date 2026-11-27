// 261127_code
// 261127_documentation

using System.Collections.Concurrent;
using EchoesOfTheShardveil.Core;

namespace EchoesOfTheShardveil.Desktop;

/// <summary>Loads card definitions from an AppData set file for use by the desktop game.</summary>
internal static class CardSet
{
    private static readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, CardDefinition>> Sets =
        new(StringComparer.Ordinal);

    /// <summary>Reads the named set and indexes its cards by their stable IDs.</summary>
    /// <param name="setName">The name shared by the set folder and its .cards file.</param>
    /// <returns>The set's cards keyed by ID.</returns>
    /// <exception cref="InvalidDataException">The file does not contain a valid card set.</exception>
    public static IReadOnlyDictionary<string, CardDefinition> Load(string setName)
    {
        if (string.IsNullOrWhiteSpace(setName) || setName is "." or ".." ||
            setName.IndexOfAny(['/', '\\', ':']) >= 0)
        {
            throw new InvalidDataException($"Invalid card set name '{setName}'.");
        }
        return Sets.GetOrAdd(setName, Read);
    }

    /// <summary>Reads and validates a set that has not been loaded yet.</summary>
    /// <param name="setName">The set name.</param>
    /// <returns>The set's cards keyed by ID.</returns>
    private static IReadOnlyDictionary<string, CardDefinition> Read(string setName)
    {
        var path = AppDataJson.PathFor("Set", setName, setName + ".cards");
        var set = AppDataJson.Load<SetData>(path);
        if (set.Name != setName || set.Cards is not { Length: > 0 })
        {
            throw new InvalidDataException($"'{path}' must contain the '{setName}' set and its cards.");
        }

        var cards = new Dictionary<string, CardDefinition>(StringComparer.Ordinal);
        foreach (var card in set.Cards)
        {
            if (card is null || string.IsNullOrWhiteSpace(card.Id) || string.IsNullOrWhiteSpace(card.Name) ||
                card.Abilities is null || card.Abilities.Any(ability => ability is null))
            {
                throw new InvalidDataException($"'{path}' contains a card with missing data.");
            }
            if (!cards.TryAdd(card.Id, card))
            {
                throw new InvalidDataException($"Duplicate card ID '{card.Id}' in '{path}'.");
            }
        }

        return cards;
    }

    /// <summary>The top-level fields in a .cards file.</summary>
    /// <param name="Name">The set name.</param>
    /// <param name="Cards">The card definitions.</param>
    private sealed record SetData(string Name, CardDefinition[] Cards);
}
