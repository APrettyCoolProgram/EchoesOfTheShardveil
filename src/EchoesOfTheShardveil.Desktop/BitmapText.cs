// 261127_code
// 261127_documentation

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EchoesOfTheShardveil.Desktop;

/// <summary>Measures, wraps and draws the ASCII glyphs shared by the board and window dialogs.</summary>
internal static class BitmapText
{
    // The font sheet is a grid of fixed-size cells in ASCII order, starting at the space character.
    private const int GlyphCellWidth = 22;
    private const int GlyphCellHeight = 32;
    private const int GlyphColumns = 16;

    /// <summary>Gets the horizontal distance from one character to the next.</summary>
    /// <param name="height">The text height in pixels.</param>
    /// <returns>The advance in pixels, at least 1.</returns>
    public static int Advance(int height) => Math.Max(1, (int)MathF.Round(height * 0.49f));

    /// <summary>Draws one line of text. Characters outside printable ASCII appear as question marks.</summary>
    /// <param name="spriteBatch">A sprite batch that has already begun.</param>
    /// <param name="font">The glyph sheet.</param>
    /// <param name="text">The text to draw.</param>
    /// <param name="x">The left edge of the text.</param>
    /// <param name="y">The top edge of the text.</param>
    /// <param name="height">The text height in pixels.</param>
    /// <param name="color">The text color.</param>
    public static void Draw(SpriteBatch spriteBatch, Texture2D font, string text, int x, int y, int height, Color color)
    {
        var advance = Advance(height);
        var glyphWidth = (int)MathF.Round(height * ((float)GlyphCellWidth / GlyphCellHeight));
        foreach (var character in text)
        {
            if (character != ' ')
            {
                var index = (character is >= ' ' and <= '~' ? character : '?') - ' ';
                var source = new Rectangle(index % GlyphColumns * GlyphCellWidth,
                    index / GlyphColumns * GlyphCellHeight, GlyphCellWidth, GlyphCellHeight);
                spriteBatch.Draw(font, new Rectangle(x, y, glyphWidth, height), source, color);
            }
            x += advance;
        }
    }

    /// <summary>Draws one line of text centered horizontally on a point.</summary>
    /// <param name="spriteBatch">A sprite batch that has already begun.</param>
    /// <param name="font">The glyph sheet.</param>
    /// <param name="text">The text to draw.</param>
    /// <param name="centerX">The horizontal center of the text.</param>
    /// <param name="y">The top edge of the text.</param>
    /// <param name="height">The text height in pixels.</param>
    /// <param name="color">The text color.</param>
    public static void DrawCentered(SpriteBatch spriteBatch, Texture2D font, string text, int centerX, int y,
        int height, Color color) =>
        Draw(spriteBatch, font, text, centerX - Advance(height) * text.Length / 2, y, height, color);

    /// <summary>Breaks text into lines at spaces.</summary>
    /// <param name="text">The text to wrap.</param>
    /// <param name="maxCharacters">The most characters on a line. A longer word is split.</param>
    /// <param name="maxLines">The most lines. A final line that does not fit is cut and ends with <c>~</c>.</param>
    /// <returns>The lines, or an empty array for empty text.</returns>
    public static string[] Wrap(string text, int maxCharacters, int maxLines = int.MaxValue)
    {
        if (text.Length == 0)
        {
            return [];
        }
        var lines = new List<string>();
        while (text.Length > maxCharacters && lines.Count < maxLines - 1)
        {
            var lastSpace = text.LastIndexOf(' ', maxCharacters);
            var split = lastSpace > 0 ? lastSpace : maxCharacters;
            lines.Add(text[..split]);
            text = text[split..].TrimStart();
        }
        lines.Add(text.Length > maxCharacters ? string.Concat(text.AsSpan(0, maxCharacters - 1), "~") : text);
        return [.. lines];
    }
}
