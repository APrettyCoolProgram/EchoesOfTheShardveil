// 261127_code
// 261127_documentation

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EchoesOfTheShardveil.Desktop;

/// <summary>Animates details in the four-world menu painting in its original pixel coordinates.</summary>
internal sealed class FourWorldMuralEffects : IDisposable
{
    private static readonly Point[] Torches = [new(191, 263), new(321, 295)];
    private static readonly Point[] Crows = [new(143, 82), new(278, 171), new(309, 176)];
    private static readonly Point[] Windows =
    [
        new(1005, 244), new(1080, 199), new(1154, 280), new(1167, 277),
        new(1304, 153), new(1336, 204), new(1395, 142), new(1488, 160),
        new(1521, 84), new(1535, 114), new(1556, 123), new(1627, 198),
        new(1505, 689), new(1560, 680), new(1616, 667), new(1647, 688)
    ];
    private static readonly (Vector2 Start, Vector2 End)[] Necromancy =
    [
        (new(272, 621), new(274, 526)), (new(261, 666), new(280, 535)),
        (new(412, 709), new(471, 520)), (new(304, 732), new(280, 535))
    ];
    private static readonly Point[] Bolt =
        [new(1199, 117), new(1185, 126), new(1180, 147), new(1173, 156), new(1183, 173),
         new(1178, 193), new(1192, 209), new(1195, 225), new(1206, 238)];

    private readonly SpriteBatch batch;
    private readonly Texture2D pixel;
    private readonly Texture2D glow;
    private readonly Texture2D mural;
    private readonly (Texture2D Texture, Rectangle Bounds, float Motion)[] cloth;
    private readonly (Texture2D Texture, Rectangle Bounds)[] skeletons;

    /// <summary>Samples soft-edged patches of cloth and hair from the painting for small, localized motion.</summary>
    /// <param name="batch">The menu sprite batch.</param>
    /// <param name="pixel">The shared solid pixel used for thin lines and particles.</param>
    /// <param name="glow">The shared soft light texture.</param>
    /// <param name="mural">The four-world painting.</param>
    public FourWorldMuralEffects(SpriteBatch batch, Texture2D pixel, Texture2D glow, Texture2D mural)
    {
        this.batch = batch;
        this.pixel = pixel;
        this.glow = glow;
        this.mural = mural;
        cloth =
        [
            ClothPatch(new(0, 491, 145, 258), 5),
            ClothPatch(new(138, 459, 63, 92), 4),
            ClothPatch(new(320, 545, 68, 139), 5),
            ClothPatch(new(494, 410, 34, 158), 4),
            ClothPatch(new(440, 605, 187, 178), 7),
            ClothPatch(new(968, 475, 120, 172), 7),
            ClothPatch(new(1191, 550, 162, 144), 8),
            ClothPatch(new(1218, 327, 112, 141), 7)
        ];
        skeletons =
        [
            RisingSkeleton(new(238, 598, 71, 83)),
            RisingSkeleton(new(278, 711, 53, 78)),
            RisingSkeleton(new(378, 679, 80, 117))
        ];
    }

    /// <summary>Creates a soft light sprite shared by all mural effects.</summary>
    /// <param name="device">The graphics device.</param>
    /// <returns>The sprite, which the caller must dispose.</returns>
    public static Texture2D CreateGlow(GraphicsDevice device)
    {
        const int size = 64;
        var pixels = new Color[size * size];
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var offset = new Vector2(x + 0.5f, y + 0.5f) / (size / 2f) - Vector2.One;
                var falloff = MathF.Max(0, 1 - offset.LengthSquared());
                pixels[y * size + x] = new Color(1f, 1f, 1f, falloff * falloff);
            }
        }

        return TextureFactory.FromPixels(device, size, size, pixels);
    }

    /// <summary>Draws the animation layers over the painting.</summary>
    /// <param name="time">Seconds since the menu opened.</param>
    /// <param name="transform">The transform from painting pixels to the viewport.</param>
    public void Draw(float time, Matrix transform)
    {
        batch.Begin(samplerState: SamplerState.LinearClamp, transformMatrix: transform);
        for (var i = 0; i < cloth.Length; i++)
        {
            var (texture, bounds, motion) = cloth[i];
            for (var y = 0; y < bounds.Height; y += 4)
            {
                var height = Math.Min(4, bounds.Height - y);
                var shift = motion * MathF.Sin(time * 3f + i - y * 0.055f) * y / bounds.Height;
                batch.Draw(texture, new Vector2(bounds.X + shift, bounds.Y + y),
                    new Rectangle(0, y, bounds.Width, height), Color.White);
            }
        }

        for (var i = 0; i < skeletons.Length; i++)
        {
            var (texture, bounds) = skeletons[i];
            var rise = MathHelper.SmoothStep(0, 6, Fade(Phase(time * 0.26f + i * 0.31f)));
            batch.Draw(texture, new Vector2(bounds.X, bounds.Y - rise), Color.White * (rise / 6f));
        }

        DrawAtmosphere(time);
        DrawCrows(time);
        DrawWindows(time);
        batch.End();

        batch.Begin(blendState: BlendState.Additive, samplerState: SamplerState.LinearClamp, transformMatrix: transform);
        DrawLights(time);
        DrawMagic(time);
        batch.End();
    }

    private void DrawAtmosphere(float time)
    {
        for (var i = 0; i < 5; i++)
        {
            var age = Phase(time * 0.11f + i * 0.2f);
            Bloom(new(35 + 350 * age, 816 + 25 * (i % 3)), new(184, 29),
                new Color(178, 188, 205), 0.22f * Fade(age), -0.08f);
        }

        for (var i = 0; i < 3; i++)
        {
            var age = Phase(time * 0.085f + i / 3f);
            Bloom(new(849 + 325 * age, 103 + i * 18), new(178, 22 + 6 * i),
                new Color(39, 49, 68), 0.56f * Fade(age), -0.21f);
        }

        for (var i = 0; i < 13; i++)
        {
            var age = Phase(time * (0.21f + i % 3 * 0.035f) + i * 0.618f);
            var position = new Vector2(823 + 426 * age, 836 + 7 * i - 18 * MathF.Sin(age * 12 + i));
            var rotation = age * MathHelper.TwoPi * 2 + i;
            Bloom(position, new(12, 2 + 4 * MathF.Abs(MathF.Sin(rotation))),
                new Color(169, 73, 38), 0.94f * Fade(age), rotation);
        }
    }

    private void DrawCrows(float time)
    {
        for (var i = 0; i < Crows.Length; i++)
        {
            var clock = (time + i * 1.96f) % (6.5f + i);
            if (clock >= 0.62f)
            {
                continue;
            }

            var center = Crows[i].ToVector2();
            var lift = MathF.Abs(MathF.Sin(clock * 19));
            var length = (i == 0 ? 25 : 15) * lift;
            var opacity = Fade(clock / 0.62f) * lift;
            for (var feather = 0; feather < 5; feather++)
            {
                var angle = 3.25f + lift * 0.72f + feather * 0.14f;
                batch.Draw(pixel, center + new Vector2(-2, -1), null,
                    new Color(18, 21, 26) * opacity, angle, new Vector2(0, 0.5f),
                    new Vector2(length * (1 - feather * 0.085f), 2.5f), SpriteEffects.None, 0);
            }
        }
    }

    private void DrawWindows(float time)
    {
        for (var i = 0; i < Windows.Length; i++)
        {
            var pane = Windows[i];
            var age = Phase(time / (3.9f + i * 0.31f) + i * 0.382f);
            var amount = Math.Max(0, 1 - MathF.Abs(age - 0.1f) * 9);
            var source = new Rectangle(pane.X - 1, pane.Y - 3, 3, 6);
            batch.Draw(mural, source, source, new Color(0.28f, 0.31f, 0.38f) * amount);
        }
    }

    private void DrawLights(float time)
    {
        var amber = new Color(255, 127, 36);
        foreach (var torch in Torches)
        {
            var flicker = 0.5f + 0.3f * MathF.Sin(time * 13 + torch.X) + 0.2f * MathF.Sin(time * 21);
            Bloom(torch.ToVector2(), new(37, 69), amber, 0.18f + 0.29f * flicker);
        }

        for (var i = 0; i < 24; i++)
        {
            var age = Phase(time * (0.59f + i % 4 * 0.14f) + i * 0.618f);
            var locus = Vector2.Lerp(new(625, 329), new(759, 554), age);
            locus.X += MathF.Sin(age * 12 + i * 7) * (4 + i % 9 * 4) * age;
            Bloom(locus, new(5 + i % 5, 11), i % 2 == 0 ? amber : Color.Yellow, 0.64f * Fade(age), -0.43f);
        }

        Bloom(new(699, 162), new(82, 108), amber, 0.12f + 0.1f * MathF.Sin(time * 7));
        Bloom(new(564, 490), new(43, 81), amber, 0.16f + 0.13f * MathF.Sin(time * 9));
        var strike = (time + 2) % 8.7f;
        var flash = MathF.Max(0, 1 - strike * 4.4f) + MathF.Max(0, 0.62f - MathF.Abs(strike - 0.32f) * 9);
        if (flash > 0)
        {
            var lightning = new Color(170, 174, 255);
            Bloom(new(1176, 175), new(240, 238), lightning, 0.31f * flash);
            for (var i = 1; i < Bolt.Length; i++)
            {
                var start = Bolt[i - 1].ToVector2();
                var direction = Bolt[i].ToVector2() - start;
                batch.Draw(pixel, start, null, Color.White * (0.74f * flash),
                    MathF.Atan2(direction.Y, direction.X), Vector2.Zero, new Vector2(direction.Length(), 1.4f),
                    SpriteEffects.None, 0);
            }
        }

        Bloom(new(1320, 291), new(100, 75), Color.Gold, 0.29f + 0.19f * MathF.Sin(time * 7));
        for (var i = 0; i < 14; i++)
        {
            var age = Phase(time * 1.7f + i * 0.618f);
            var nozzle = i % 2 == 0 ? new Vector2(1409, 593) : new Vector2(1534, 561);
            var jet = nozzle + new Vector2(-30 * age + i % 4 * 3, age * 68);
            Bloom(jet, new(9, 28), i % 3 == 0 ? Color.LightYellow : amber, 0.54f * Fade(age), 0.35f);
        }
    }

    private void DrawMagic(float time)
    {
        var blue = new Color(75, 154, 255);
        var gold = new Color(255, 206, 73);
        var purple = new Color(172, 91, 255);
        var orb = new Vector2(78, 379);
        for (var i = 0; i < 9; i++)
        {
            var angle = time * 3.5f + i * MathHelper.TwoPi / 9;
            Bloom(orb + new Vector2(MathF.Cos(angle) * 23, MathF.Sin(angle) * 15),
                new(13, 8), i % 2 == 0 ? blue : gold, 0.55f, angle);
        }

        var strength = 0.55f + 0.45f * MathF.Sin(time * 4.5f);
        Bloom(new(292, 462), new(104 + 18 * strength, 98 + 18 * strength), blue, 0.23f + 0.31f * strength);
        Bloom(new(292, 462), new(31, 32), Color.White, 0.31f + 0.42f * strength);

        foreach (var (start, end) in Necromancy)
        {
            for (var i = 0; i < 9; i++)
            {
                var age = Phase(time * 0.72f + i / 9f);
                var point = Vector2.Lerp(start, end, age);
                point.X += MathF.Sin(age * 14 + time * 2 + start.X) * Fade(age) * 14;
                Bloom(point, new(13, 30), purple, 0.34f * Fade(age), 0.24f);
            }
        }

        for (var i = 0; i < 13; i++)
        {
            var age = Phase(time * 0.7f + i * 0.618f);
            var glove = i % 2 == 0 ? new Vector2(1481, 385) : new Vector2(1657, 430);
            var point = glove + new Vector2(-58 * age, -47 * age + 16 * MathF.Sin(age * 8 + i));
            Bloom(point, new(34 + 33 * age, 22 + 23 * age),
                new Color(128, 236, 69), 0.24f * Fade(age), -0.43f);
        }
    }

    private (Texture2D Texture, Rectangle Bounds, float Motion) ClothPatch(Rectangle bounds, float motion) =>
        (Sample(bounds), bounds, motion);

    private (Texture2D Texture, Rectangle Bounds) RisingSkeleton(Rectangle bounds) =>
        (Sample(bounds), bounds);

    private Texture2D Sample(Rectangle bounds)
    {
        var pixels = new Color[bounds.Width * bounds.Height];
        mural.GetData(0, bounds, pixels, 0, pixels.Length);
        for (var y = 0; y < bounds.Height; y++)
        {
            for (var x = 0; x < bounds.Width; x++)
            {
                var offset = new Vector2(2f * x / bounds.Width - 1, 2f * y / bounds.Height - 1);
                var feather = MathHelper.SmoothStep(0, 1, Math.Clamp((1 - offset.LengthSquared()) * 2.6f, 0, 1));
                pixels[y * bounds.Width + x] *= feather * 0.86f;
            }
        }
        return TextureFactory.FromPixels(mural.GraphicsDevice, bounds.Width, bounds.Height, pixels);
    }

    private void Bloom(Vector2 point, Vector2 size, Color color, float opacity, float rotation = 0f) =>
        batch.Draw(glow, point, null, color * opacity, rotation, new Vector2(glow.Width, glow.Height) / 2,
            size / new Vector2(glow.Width, glow.Height), SpriteEffects.None, 0);

    private static float Phase(float clock) => clock - MathF.Floor(clock);

    private static float Fade(float phase) => MathF.Sin(MathF.PI * phase);

    /// <summary>Releases the sampled animation textures.</summary>
    public void Dispose()
    {
        foreach (var (texture, _, _) in cloth)
        {
            texture.Dispose();
        }
        foreach (var (texture, _) in skeletons)
        {
            texture.Dispose();
        }
    }
}
