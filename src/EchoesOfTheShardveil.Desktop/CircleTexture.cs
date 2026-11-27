// 261127_code
// 261127_documentation

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EchoesOfTheShardveil.Desktop;

/// <summary>Builds antialiased circular textures with a white border.</summary>
internal static class CircleTexture
{
    /// <summary>Crops a square portrait to a circle and adds a border.</summary>
    /// <param name="portrait">A square texture with premultiplied alpha.</param>
    /// <param name="displaySize">The size at which the result will be drawn.</param>
    /// <param name="borderWidth">The border thickness at the display size.</param>
    /// <returns>A new texture the size of the portrait, which the caller must dispose.</returns>
    public static Texture2D FramePortrait(Texture2D portrait, int displaySize, int borderWidth)
    {
        var size = portrait.Width;
        var source = new Color[size * size];
        portrait.GetData(source);
        return TextureFactory.FromPixels(portrait.GraphicsDevice, size, size,
            Render(size, borderWidth * (float)size / displaySize, Color.Black, source));
    }

    /// <summary>Creates a bordered disc at twice the display size, so it stays smooth when scaled.</summary>
    /// <param name="device">The graphics device that will own the texture.</param>
    /// <param name="displaySize">The diameter at which the result will be drawn.</param>
    /// <param name="borderWidth">The border thickness at the display size.</param>
    /// <param name="fill">The premultiplied color inside the border.</param>
    /// <returns>A new texture the caller must dispose.</returns>
    public static Texture2D Create(GraphicsDevice device, int displaySize, int borderWidth, Color fill)
    {
        var size = displaySize * 2;
        return TextureFactory.FromPixels(device, size, size, Render(size, borderWidth * 2f, fill, []));
    }

    /// <summary>Computes the pixels of a bordered, antialiased disc.</summary>
    /// <param name="size">The width and height in pixels.</param>
    /// <param name="borderWidth">The thickness of the white border.</param>
    /// <param name="fill">The color inside the border when there is no portrait.</param>
    /// <param name="portrait">Square premultiplied pixels to show inside the border, or empty.</param>
    /// <returns>The pixels in row-major order.</returns>
    private static Color[] Render(int size, float borderWidth, Color fill, ReadOnlySpan<Color> portrait)
    {
        var pixels = new Color[size * size];
        var radius = size / 2f;
        for (var y = 0; y < size; y++)
        {
            var dy = y + 0.5f - radius;
            for (var x = 0; x < size; x++)
            {
                var dx = x + 0.5f - radius;
                var distance = MathF.Sqrt(dx * dx + dy * dy);
                var coverage = Math.Clamp(radius - distance + 0.5f, 0f, 1f);
                if (coverage == 0f)
                {
                    continue;
                }

                var index = y * size + x;
                var inner = Math.Clamp(radius - borderWidth - distance + 0.5f, 0f, 1f);
                var edge = coverage - inner;
                // The source portrait is premultiplied, so compositing it over black only changes its alpha.
                var face = portrait.IsEmpty ? fill : new Color(portrait[index].R, portrait[index].G, portrait[index].B, (byte)255);
                pixels[index] = new Color(
                    (int)MathF.Round(255 * edge + face.R * inner),
                    (int)MathF.Round(255 * edge + face.G * inner),
                    (int)MathF.Round(255 * edge + face.B * inner),
                    (int)MathF.Round(255 * edge + face.A * inner));
            }
        }
        return pixels;
    }
}
