using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TheCards.Desktop;

/// <summary>Builds antialiased circular textures with a white border.</summary>
internal static class CircleTexture
{
    public static Texture2D FramePortrait(Texture2D portrait, int displaySize, int borderWidth)
    {
        var size = portrait.Width;
        var source = new Color[size * size];
        portrait.GetData(source);
        return Upload(portrait.GraphicsDevice, size,
            Render(size, borderWidth * (float)size / displaySize, Color.Black, source));
    }

    public static Texture2D Create(GraphicsDevice device, int displaySize, int borderWidth, Color fill)
    {
        var size = displaySize * 2;
        return Upload(device, size, Render(size, borderWidth * 2f, fill, []));
    }

    internal static Color[] Render(int size, float borderWidth, Color fill, ReadOnlySpan<Color> portrait)
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

    private static Texture2D Upload(GraphicsDevice device, int size, Color[] pixels)
    {
        var texture = new Texture2D(device, size, size);
        texture.SetData(pixels);
        return texture;
    }
}
