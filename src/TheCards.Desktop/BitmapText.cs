using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TheCards.Desktop;

/// <summary>Draws the ASCII glyphs shared by the board and window dialogs.</summary>
internal static class BitmapText
{
    public static int Advance(int height) => Math.Max(1, (int)MathF.Round(height * 0.49f));

    public static void Draw(SpriteBatch batch, Texture2D font, string text, int x, int y, int height, Color color)
    {
        var advance = Advance(height);
        var glyphWidth = (int)MathF.Round(height * (22f / 32f));
        foreach (var character in text)
        {
            if (character != ' ')
            {
                var index = (character is >= ' ' and <= '~' ? character : '?') - ' ';
                var source = new Rectangle(index % 16 * 22, index / 16 * 32, 22, 32);
                batch.Draw(font, new Rectangle(x, y, glyphWidth, height), source, color);
            }
            x += advance;
        }
    }
}
