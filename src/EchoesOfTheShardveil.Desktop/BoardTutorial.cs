// 261127_code
// 261127_documentation

using System.Globalization;
using EchoesOfTheShardveil.Core;

namespace EchoesOfTheShardveil.Desktop;

/// <summary>Tracks the optional pre-match rules walkthrough.</summary>
internal sealed class BoardTutorial
{
    private static readonly Lazy<TutorialContent> Content = new(LoadContent);

    /// <summary>Gets the lessons in the order they are shown.</summary>
    public static IReadOnlyList<TutorialPage> Pages => Content.Value.Pages;

    /// <summary>Gets the example Unit shown in the card illustration.</summary>
    public static CardDefinition ExampleCard => Content.Value.ExampleCard;

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

    /// <summary>Loads the Alpha tutorial, validates its pages, and fills in current rules values.</summary>
    /// <returns>The resolved tutorial pages and example card.</returns>
    private static TutorialContent LoadContent()
    {
        var path = AppDataJson.PathFor("Tutorial", "Alpha.json");
        var tutorial = AppDataJson.Load<TutorialData>(path);
        if (string.IsNullOrWhiteSpace(tutorial.ExampleCardId) || tutorial.Pages is not { Length: > 0 } ||
            tutorial.Pages.Any(page => page is null || string.IsNullOrWhiteSpace(page.Title) ||
                page.Points is not { Count: > 0 } || page.Points.Any(string.IsNullOrWhiteSpace)))
        {
            throw new InvalidDataException($"'{path}' must contain an example card ID and complete tutorial pages.");
        }

        var example = StarterDecks.GetCard(tutorial.ExampleCardId);
        if (example.Type != CardType.Unit || !Duel.IsPlayableCard(example))
        {
            throw new InvalidDataException($"The example card in '{path}' must be a playable Unit.");
        }

        var format = MatchFormats.Active;
        var tokens = new (string Token, int Value)[]
        {
            ("{startingHealth}", format.StartingHealth),
            ("{deckSize}", format.DeckSize),
            ("{maxCopiesPerName}", format.MaxCopiesPerName),
            ("{openingHandSize}", Duel.OpeningHandSize),
            ("{unitSlots}", Duel.UnitSlots),
            ("{relicSlots}", Duel.RelicSlots),
            ("{handLimit}", Duel.HandLimit),
        };
        var pages = tutorial.Pages.Select(page => page with
        {
            Points = page.Points.Select(point => tokens.Aggregate(point, (text, token) =>
                text.Replace(token.Token, token.Value.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)))
                .ToArray(),
        }).ToArray();
        return new TutorialContent(pages, example);
    }

    /// <summary>The tutorial content as stored in AppData.</summary>
    /// <param name="ExampleCardId">The ID of the example Unit in the Alpha set.</param>
    /// <param name="Pages">The lessons in display order.</param>
    private sealed record TutorialData(string ExampleCardId, TutorialPage[] Pages);

    /// <summary>The resolved tutorial available to the board.</summary>
    /// <param name="Pages">The lessons with rules values filled in.</param>
    /// <param name="ExampleCard">The Unit shown in the card illustration.</param>
    private sealed record TutorialContent(TutorialPage[] Pages, CardDefinition ExampleCard);
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
