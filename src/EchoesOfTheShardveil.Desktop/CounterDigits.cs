// 261127_code
// 261127_documentation

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EchoesOfTheShardveil.Desktop;

/// <summary>Builds the small digit textures used on the card-shaped counters.</summary>
internal static class CounterDigits
{
    // Sheet pixels darker than this are treated as background.
    private const int InkThreshold = 24;

    /// <summary>
    /// Shrinks the regular-weight sans-serif digits 0-9 from a one-row sheet to counter size with an area
    /// average. Every digit texture has the same height and an odd width, so numbers share a baseline and,
    /// in a counter with odd dimensions, center exactly.
    /// </summary>
    /// <param name="sheet">Ten equal cells holding white digits on black, 0 first.</param>
    /// <param name="digitHeight">The height of the textures to build.</param>
    /// <returns>Ten textures indexed by digit, which the caller must dispose.</returns>
    public static Texture2D[] Build(Texture2D sheet, int digitHeight)
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
                    if (pixels[y * sheet.Width + digit * cellWidth + x].R < InkThreshold)
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

        var scale = (float)digitHeight / (bottom - top);
        var textures = new Texture2D[glyphs.Length];
        for (var digit = 0; digit < glyphs.Length; digit++)
        {
            var glyph = glyphs[digit];
            // Round the width to the nearest odd number.
            var width = Math.Max(1, (int)MathF.Round((glyph.Width * scale - 1) / 2) * 2 + 1);
            var output = new Color[width * digitHeight];
            for (var y = 0; y < digitHeight; y++)
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
            textures[digit] = TextureFactory.FromPixels(sheet.GraphicsDevice, width, digitHeight, output);
        }
        return textures;
    }
}
