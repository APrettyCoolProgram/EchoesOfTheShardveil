// 261127_code
// 261127_documentation

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EchoesOfTheShardveil.Desktop;

/// <summary>Animates details in the ultrawide artwork using coordinates in the source image.</summary>
internal sealed class SideSceneEffects : IDisposable
{
    private const int SectorCount = 8;
    private static readonly Rectangle GillArea = new(754, 516, 270, 312);
    private static readonly Rectangle TreeArea = new(4, 42, 421, 456);
    private const int LightCellWidth = 24;
    private const int LightCellHeight = 40;
    private const int LightAtlasColumns = 8;
    // Centers and radii of the painted windows and street lights in the distant city.
    private static readonly (int X, int Y, int Rx, int Ry)[] CityLights =
    [
        (820, 415, 9, 17), (820, 457, 10, 14), (829, 424, 5, 10), (837, 422, 4, 8),
        (796, 432, 4, 7), (812, 438, 4, 6), (790, 461, 7, 9), (779, 462, 4, 5),
        (800, 454, 5, 7), (834, 453, 4, 6), (841, 453, 4, 6), (850, 452, 5, 9),
        (861, 451, 4, 6), (868, 454, 4, 5), (876, 449, 4, 7), (882, 454, 3, 4),
        (712, 454, 4, 6), (731, 463, 4, 5), (715, 469, 3, 4), (725, 475, 4, 4),
        (733, 477, 3, 4), (739, 479, 3, 4), (749, 482, 4, 4),
        (682, 467, 3, 4), (690, 472, 3, 4), (700, 472, 4, 5), (700, 480, 3, 4),
        (684, 482, 4, 4), (704, 487, 7, 6), (716, 483, 3, 4), (716, 489, 3, 4),
        (729, 485, 4, 4), (741, 487, 4, 4), (713, 496, 4, 4), (727, 494, 4, 5),
        (736, 496, 4, 4), (748, 503, 4, 4), (658, 494, 3, 4), (668, 497, 3, 4),
        (677, 502, 3, 4), (682, 504, 3, 4), (687, 505, 3, 4), (696, 507, 3, 5),
        (701, 507, 3, 5), (706, 508, 3, 5), (712, 509, 3, 5), (716, 510, 3, 4),
        (655, 521, 3, 4), (672, 518, 4, 4), (698, 520, 3, 4),
        (727, 524, 5, 5), (743, 528, 4, 5), (752, 531, 3, 4),
        (777, 542, 5, 6), (791, 550, 5, 5),
    ];
    private static readonly Vector2[] Wisps = [new(312, 653), new(281, 816), new(254, 902)];
    private static readonly Rectangle ForestFogArea = new(25, 335, 475, 869);
    private static readonly (Vector2 Center, Vector2 Radius, float Angle)[] FogBanks =
    [
        (new(277, 418), new(123, 64), -0.42f),
        (new(268, 535), new(157, 82), -0.46f),
        (new(219, 638), new(150, 82), -0.18f),
        (new(111, 745), new(93, 48), -0.34f),
        (new(180, 797), new(140, 57), 0.20f),
        (new(329, 811), new(135, 50), -0.54f),
        (new(307, 879), new(131, 58), -0.38f),
        (new(220, 954), new(144, 45), -0.28f),
        (new(170, 1042), new(104, 32), -0.20f),
        (new(237, 1130), new(138, 42), 0.30f),
    ];
    private static readonly (Vector2 Center, Vector2[] Outline)[] Gills =
    [
        (new(923, 586), [new(793, 596), new(845, 560), new(911, 533), new(977, 524), new(1023, 528),
            new(1023, 573), new(1007, 589), new(969, 601), new(950, 601), new(937, 586),
            new(919, 590), new(904, 605), new(870, 618), new(831, 624), new(800, 620), new(788, 610)]),
        (new(842, 748), [new(765, 745), new(795, 733), new(838, 724), new(881, 727), new(906, 736),
            new(904, 745), new(881, 754), new(854, 757), new(842, 748), new(830, 756),
            new(805, 766), new(779, 766), new(762, 759)]),
        (new(932, 777), [new(886, 769), new(909, 761), new(940, 766), new(972, 774), new(983, 782),
            new(979, 787), new(949, 784), new(934, 778), new(918, 783), new(893, 776)]),
        (new(825, 818), [new(805, 819), new(820, 814), new(838, 814), new(844, 817), new(827, 822)]),
    ];
    private static readonly Color[] LeafColors = [new(182, 116, 62), new(154, 77, 43), new(201, 142, 75), new(102, 118, 115)];
    // Cross-sections inside the painted path, in source artwork coordinates.
    private static readonly (int Y, int Left, int Right)[] PathCrossings =
    [
        (918, 247, 347),
        (956, 215, 320),
        (991, 176, 282),
        (1028, 133, 242),
        (1071, 145, 249),
        (1118, 172, 277),
        (1165, 208, 311),
        (1214, 236, 348),
        (1262, 233, 360),
    ];
    private readonly Texture2D leaf;
    private readonly Texture2D gillMasks;
    private readonly Texture2D treeMask;
    private readonly Texture2D atmosphere;
    private readonly Texture2D forestFog;
    private readonly Texture2D cityLightsOff;
    private readonly int[] flickeringCityLights;

    /// <summary>Builds the masks and sprites the animations need from the side artwork.</summary>
    /// <param name="artwork">The two side panels. The caller keeps ownership.</param>
    public SideSceneEffects(Texture2D artwork)
    {
        gillMasks = CreateGillMasks(artwork);
        leaf = CreateLeaf(artwork.GraphicsDevice);
        treeMask = CreateTreeMask(artwork);
        atmosphere = CreateAtmosphere(artwork.GraphicsDevice);
        forestFog = CreateForestFog(artwork);
        cityLightsOff = CreateCityLightsOff(artwork, out flickeringCityLights);
    }

    /// <summary>Ripples the forest tree and animates the mist and lights around the ruins.</summary>
    /// <param name="spriteBatch">A sprite batch that has already begun.</param>
    /// <param name="crop">The visible part of the artwork.</param>
    /// <param name="transform">The transform from artwork to screen.</param>
    /// <param name="time">Seconds of animation.</param>
    public void DrawForest(SpriteBatch spriteBatch, Rectangle crop, Matrix transform, float time)
    {
        var scale = ScaleOf(transform);
        var visible = Rectangle.Intersect(crop, TreeArea);
        // Give the moving strips room to shift without crossing the panel boundary.
        visible.Inflate(-5, 0);
        if (visible.Width > 0)
        {
            for (var y = visible.Top; y < visible.Bottom; y += 3)
            {
                var height = Math.Min(3, visible.Bottom - y);
                var sway = 2.5f * MathF.Sin(y * 0.031f - time * 1.35f)
                    + 0.9f * MathF.Sin(y * 0.069f - time * 0.8f);
                var row = new Rectangle(visible.X - TreeArea.X, y - TreeArea.Y, visible.Width, height);
                var position = Vector2.Transform(new Vector2(visible.X + sway, y), transform);
                spriteBatch.Draw(treeMask, position, row, Color.White, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
            }
        }

        var fog = Rectangle.Intersect(crop, ForestFogArea);
        fog.Inflate(-12, -5);
        if (fog.Width > 0 && fog.Height > 0)
        {
            for (var y = fog.Top; y < fog.Bottom; y += 3)
            {
                var height = Math.Min(3, fog.Bottom - y);
                var phase = time * 0.65f - y * 0.012f;
                var curl = 7f * MathF.Sin(phase) + 3f * MathF.Sin(phase * 1.8f + 1.3f);
                var row = new Rectangle(fog.X - ForestFogArea.X, y - ForestFogArea.Y, fog.Width, height);
                var position = Vector2.Transform(new Vector2(fog.X + curl, y), transform);
                spriteBatch.Draw(forestFog, position, row, Color.White * (0.78f + 0.19f * MathF.Sin(phase)),
                    0, Vector2.Zero, scale, SpriteEffects.None, 0);
            }
        }

        for (var i = 0; i < FogBanks.Length; i++)
        {
            var (center, radius, angle) = FogBanks[i];
            var phase = time * (0.29f + i % 3 * 0.08f) + i * 2.1f;
            var drift = new Vector2(MathF.Cos(phase) * 24, MathF.Sin(phase) * 9);
            DrawHaze(spriteBatch, crop, transform, center + drift, new Vector2(radius.X * 0.68f, radius.Y * 0.55f),
                new Color(119, 169, 182) * (0.12f + 0.045f * MathF.Sin(phase)), angle + 0.22f * MathF.Cos(phase));
        }

        for (var i = 0; i < Wisps.Length; i++)
        {
            var pulse = 0.5f + 0.5f * MathF.Sin(time * MathHelper.TwoPi / (8.5f + i * 0.7f) + i * 2.1f);
            var tint = new Color(89, 224, 255);
            DrawHaze(spriteBatch, crop, transform, Wisps[i], new Vector2(30, 47) * (0.92f + 0.12f * pulse),
                tint * (0.12f + 0.32f * pulse));
            DrawHaze(spriteBatch, crop, transform, Wisps[i], new Vector2(9, 18),
                new Color(178, 247, 255) * (0.29f + 0.38f * pulse));
        }
    }

    /// <summary>Flickers the painted warm window and street lights in the distant fairy city.</summary>
    /// <param name="spriteBatch">A sprite batch that has already begun.</param>
    /// <param name="crop">The visible part of the artwork.</param>
    /// <param name="transform">The transform from artwork to screen.</param>
    /// <param name="time">Seconds of animation.</param>
    public void DrawCityLights(SpriteBatch spriteBatch, Rectangle crop, Matrix transform, float time)
    {
        const float pauseDuration = 1f;
        const float offDuration = 1.25f;
        const float interval = pauseDuration + offDuration;
        if (flickeringCityLights.Length == 0 || time % interval < pauseDuration)
        {
            return;
        }

        var step = (int)(time / interval);
        var index = flickeringCityLights[step % flickeringCityLights.Length];
        var light = CityLightBounds(index);
        var visible = Rectangle.Intersect(crop, light);
        if (visible.Width == 0 || visible.Height == 0)
        {
            return;
        }

        var source = visible;
        source.Offset(index % LightAtlasColumns * LightCellWidth - light.X,
            index / LightAtlasColumns * LightCellHeight - light.Y);
        spriteBatch.Draw(cityLightsOff, Vector2.Transform(visible.Location.ToVector2(), transform), source,
            Color.White, 0, Vector2.Zero, ScaleOf(transform), SpriteEffects.None, 0);
    }

    /// <summary>Draws a soft patch of light or mist, fading it out near the edge of the visible artwork.</summary>
    /// <param name="spriteBatch">A sprite batch that has already begun.</param>
    /// <param name="crop">The visible part of the artwork.</param>
    /// <param name="transform">The transform from artwork to screen.</param>
    /// <param name="center">The patch's center in the artwork.</param>
    /// <param name="size">The patch's size in the artwork.</param>
    /// <param name="tint">The color.</param>
    /// <param name="angle">The rotation in radians.</param>
    private void DrawHaze(SpriteBatch spriteBatch, Rectangle crop, Matrix transform, Vector2 center, Vector2 size,
        Color tint, float angle = 0)
    {
        var radius = size.Length() / 2;
        var fade = Math.Clamp((DistanceToEdge(crop, center) - radius) / 12, 0, 1);
        if (fade <= 0)
        {
            return;
        }

        var scale = size * new Vector2(transform.M11 / atmosphere.Width, transform.M22 / atmosphere.Height);
        spriteBatch.Draw(atmosphere, Vector2.Transform(center, transform), null, tint * fade, angle,
            new Vector2(atmosphere.Width / 2f, atmosphere.Height / 2f), scale, SpriteEffects.None, 0);
    }

    /// <summary>Draws tumbling leaves blowing left to right across the forest path.</summary>
    /// <param name="spriteBatch">A sprite batch that has already begun.</param>
    /// <param name="source">The visible part of the artwork.</param>
    /// <param name="transform">The transform from artwork to screen.</param>
    /// <param name="time">Seconds of animation.</param>
    public void DrawLeaves(SpriteBatch spriteBatch, Rectangle source, Matrix transform, float time)
    {
        var scale = ScaleOf(transform);
        for (var i = 0; i < PathCrossings.Length; i++)
        {
            var (y, left, right) = PathCrossings[i];
            var depth = i / (float)(PathCrossings.Length - 1);
            var size = MathHelper.Lerp(0.36f, 0.82f, depth) * (0.9f + i % 3 * 0.08f);
            var radius = leaf.Width * size / 2 + 2;
            var clock = time / (3.5f + i % 4 * 0.6f) + i * 0.618f;
            var progress = clock - MathF.Floor(clock);
            var travel = Math.Clamp(progress / 0.67f, 0, 1);
            var gust = travel - 0.11f * MathF.Sin(travel * MathHelper.TwoPi);
            var bounce = MathF.Abs(MathF.Sin(travel * 18 + i)) * MathHelper.Lerp(1.5f, 5f, depth);
            var position = new Vector2(MathHelper.Lerp(left + radius, right - radius, gust), y - bounce);
            var opacity = Math.Clamp((DistanceToEdge(source, position) - radius) / 10, 0, 1) *
                Math.Clamp(Math.Min(travel, 1 - travel) * 10, 0, 1);
            if (opacity <= 0)
            {
                continue;
            }

            var tumble = travel * (9.5f + i % 4 * 1.4f) + i * 2.4f;
            var leafScale = scale * new Vector2(size, size * (0.2f + 0.8f * MathF.Abs(MathF.Sin(tumble))));
            spriteBatch.Draw(leaf, Vector2.Transform(position, transform), null, LeafColors[i % LeafColors.Length] * opacity,
                tumble, new Vector2(leaf.Width / 2f, leaf.Height / 2f), leafScale, SpriteEffects.None, 0);
        }
    }

    /// <summary>Sweeps color clockwise around the painted gills without moving the caps or stems.</summary>
    /// <param name="spriteBatch">A sprite batch that has already begun.</param>
    /// <param name="crop">The visible part of the artwork.</param>
    /// <param name="transform">The transform from artwork to screen.</param>
    /// <param name="time">Seconds of animation.</param>
    public void DrawGills(SpriteBatch spriteBatch, Rectangle crop, Matrix transform, float time)
    {
        var visible = Rectangle.Intersect(crop, GillArea);
        if (visible.Width == 0 || visible.Height == 0)
        {
            return;
        }

        var position = Vector2.Transform(visible.Location.ToVector2(), transform);
        var scale = ScaleOf(transform);
        for (var sector = 0; sector < SectorCount; sector++)
        {
            var phase = (sector / (float)SectorCount - time / 12.8f) * MathHelper.TwoPi;
            var color = new Color(
                0.54f + 0.46f * MathF.Cos(phase),
                0.54f + 0.46f * MathF.Cos(phase - MathHelper.TwoPi / 3),
                0.54f + 0.46f * MathF.Cos(phase + MathHelper.TwoPi / 3));
            var mask = new Rectangle(visible.X - GillArea.X, visible.Y - GillArea.Y + sector * GillArea.Height,
                visible.Width, visible.Height);
            spriteBatch.Draw(gillMasks, position, mask, color * 0.64f, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
        }
    }

    /// <summary>Splits the painted gills into angular sectors, one mask per sector.</summary>
    /// <param name="artwork">The side artwork.</param>
    /// <returns>The masks stacked vertically.</returns>
    private static Texture2D CreateGillMasks(Texture2D artwork)
    {
        var stride = GillArea.Width * GillArea.Height;
        var pixels = new Color[stride];
        artwork.GetData(0, GillArea, pixels, 0, pixels.Length);
        var masks = new Color[stride * SectorCount];
        for (var y = 0; y < GillArea.Height; y++)
            for (var x = 0; x < GillArea.Width; x++)
            {
                var point = new Vector2(GillArea.X + x + 0.5f, GillArea.Y + y + 0.5f);
                var index = y * GillArea.Width + x;
                foreach (var (center, outline) in Gills)
                {
                    var coverage = OutlineCoverage(point, outline);
                    if (coverage <= 0)
                    {
                        continue;
                    }

                    var offset = point - center;
                    var angle = MathF.Atan2(offset.Y * 2.7f, offset.X);
                    var phase = (angle / MathHelper.TwoPi + 1) * SectorCount;
                    var lower = (int)phase;
                    var fraction = phase - lower;
                    var light = coverage * (0.20f + 0.80f * pixels[index].G / 255);
                    masks[lower % SectorCount * stride + index] = Color.White * (light * (1 - fraction));
                    masks[(lower + 1) % SectorCount * stride + index] = Color.White * (light * fraction);
                    break;
                }
            }

        return TextureFactory.FromPixels(artwork.GraphicsDevice, GillArea.Width, GillArea.Height * SectorCount, masks);
    }

    /// <summary>Gets the scale of an axis-aligned transform.</summary>
    /// <param name="transform">The transform.</param>
    /// <returns>The horizontal and vertical scale.</returns>
    private static Vector2 ScaleOf(Matrix transform) => new(transform.M11, transform.M22);

    /// <summary>Measures how far a point is inside a rectangle.</summary>
    /// <param name="bounds">The rectangle.</param>
    /// <param name="point">The point.</param>
    /// <returns>The distance to the nearest side, negative outside.</returns>
    private static float DistanceToEdge(Rectangle bounds, Vector2 point) =>
        Math.Min(Math.Min(point.X - bounds.Left, bounds.Right - point.X),
            Math.Min(point.Y - bounds.Top, bounds.Bottom - point.Y));

    /// <summary>Rates how far inside a gill outline a point is, for a soft edge.</summary>
    /// <param name="point">The point.</param>
    /// <param name="outline">The corners of the outline.</param>
    /// <returns>From 0 outside to 1 well inside.</returns>
    private static float OutlineCoverage(Vector2 point, Vector2[] outline)
    {
        if (!Geometry.IsInPolygon(point, outline))
        {
            return 0;
        }

        var distanceSquared = float.PositiveInfinity;
        for (var i = 0; i < outline.Length; i++)
        {
            distanceSquared = Math.Min(distanceSquared,
                Geometry.DistanceSquaredToSegment(point, outline[i], outline[(i + 1) % outline.Length]));
        }

        return Math.Clamp(MathF.Sqrt(distanceSquared) / 3, 0, 1);
    }

    /// <summary>Draws a small leaf sprite.</summary>
    /// <param name="device">The graphics device.</param>
    /// <returns>The sprite.</returns>
    private static Texture2D CreateLeaf(GraphicsDevice device)
    {
        const int width = 24;
        const int height = 12;
        var pixels = new Color[width * height];
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var along = (x + 0.5f) / width;
                var across = y + 0.5f - height / 2f;
                var edge = 4.5f * MathF.Sin(along * MathHelper.Pi);
                var coverage = Math.Clamp(edge - MathF.Abs(across), 0, 1);
                var shade = MathF.Abs(across) < 1 ? 1f : 0.78f - 0.14f * across / height;
                pixels[y * width + x] = new Color(shade, shade, shade) * coverage;
            }

        return TextureFactory.FromPixels(device, width, height, pixels);
    }

    /// <summary>Selects the dark bark of the forest tree, fading toward the area's edges.</summary>
    /// <param name="artwork">The side artwork.</param>
    /// <returns>The tree texture.</returns>
    private static Texture2D CreateTreeMask(Texture2D artwork)
    {
        var pixels = new Color[TreeArea.Width * TreeArea.Height];
        artwork.GetData(0, TreeArea, pixels, 0, pixels.Length);
        for (var y = 0; y < TreeArea.Height; y++)
            for (var x = 0; x < TreeArea.Width; x++)
            {
                var index = y * TreeArea.Width + x;
                var pixel = pixels[index];
                var bark = Math.Clamp((94 - Math.Max(pixel.R, pixel.G)) / 52f, 0, 1);
                var horizontal = Math.Clamp(Math.Min(x, TreeArea.Width - 1 - x) / 38f, 0, 1);
                var vertical = Math.Clamp(Math.Min(y, TreeArea.Height - 1 - y) / 52f, 0, 1);
                pixels[index] = pixel * (bark * horizontal * vertical * 0.72f);
            }

        return TextureFactory.FromPixels(artwork.GraphicsDevice, TreeArea.Width, TreeArea.Height, pixels);
    }

    /// <summary>Draws the soft round sprite used for haze.</summary>
    /// <param name="device">The graphics device.</param>
    /// <returns>The sprite.</returns>
    private static Texture2D CreateAtmosphere(GraphicsDevice device)
    {
        const int size = 32;
        var pixels = new Color[size * size];
        for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var point = new Vector2(x + 0.5f, y + 0.5f) / (size / 2f) - Vector2.One;
                var falloff = Math.Max(0, 1 - point.LengthSquared());
                pixels[y * size + x] = Color.White * (falloff * falloff);
            }

        return TextureFactory.FromPixels(device, size, size, pixels);
    }

    /// <summary>Selects the pale painted fog inside the fog banks, leaving the wisps clear.</summary>
    /// <param name="artwork">The side artwork.</param>
    /// <returns>The fog texture.</returns>
    private static Texture2D CreateForestFog(Texture2D artwork)
    {
        var pixels = new Color[ForestFogArea.Width * ForestFogArea.Height];
        artwork.GetData(0, ForestFogArea, pixels, 0, pixels.Length);
        var coverage = new float[pixels.Length];
        foreach (var (center, radius, angle) in FogBanks)
        {
            var rotation = Matrix.CreateRotationZ(-angle);
            for (var y = 0; y < ForestFogArea.Height; y++)
                for (var x = 0; x < ForestFogArea.Width; x++)
                {
                    var point = new Vector2(ForestFogArea.X + x, ForestFogArea.Y + y);
                    var local = Vector2.Transform(point - center, rotation) / radius;
                    var feather = Math.Clamp((1 - local.LengthSquared()) * 1.5f, 0, 1);
                    var index = y * ForestFogArea.Width + x;
                    coverage[index] = Math.Max(coverage[index], feather * feather);
                }
        }

        for (var y = 0; y < ForestFogArea.Height; y++)
            for (var x = 0; x < ForestFogArea.Width; x++)
            {
                var index = y * ForestFogArea.Width + x;
                var pixel = pixels[index];
                var cool = Math.Clamp((Math.Min(pixel.G, pixel.B) - pixel.R + 8) / 33f, 0, 1);
                var luminance = Math.Clamp((pixel.G - 42) / 76f, 0, 1);
                var point = new Vector2(ForestFogArea.X + x, ForestFogArea.Y + y);
                var wispClearance = 1f;
                foreach (var wisp in Wisps)
                {
                    wispClearance = Math.Min(wispClearance,
                        Math.Clamp((Vector2.DistanceSquared(point, wisp) - 13 * 13) / (25 * 25 - 13 * 13f), 0, 1));
                }
                pixels[index] = pixel * (coverage[index] * cool * luminance * wispClearance * 0.74f);
            }

        return TextureFactory.FromPixels(artwork.GraphicsDevice, ForestFogArea.Width, ForestFogArea.Height, pixels);
    }

    /// <summary>Gets the area of a painted city light.</summary>
    /// <param name="index">The light's index.</param>
    /// <returns>The area in the artwork.</returns>
    private static Rectangle CityLightBounds(int index)
    {
        var (x, y, rx, ry) = CityLights[index];
        return new Rectangle(x - rx, y - ry, 2 * rx + 1, 2 * ry + 1);
    }

    /// <summary>Builds unlit versions of the painted city lights.</summary>
    /// <param name="artwork">The side artwork.</param>
    /// <param name="flickeringLights">The lights that turned out to be painted, in shuffled flicker order.</param>
    /// <returns>An atlas with one cell per light.</returns>
    private static Texture2D CreateCityLightsOff(Texture2D artwork, out int[] flickeringLights)
    {
        const int width = LightAtlasColumns * LightCellWidth;
        var height = (CityLights.Length + LightAtlasColumns - 1) / LightAtlasColumns * LightCellHeight;
        var atlas = new Color[width * height];
        var paintedLights = new List<int>(CityLights.Length);
        for (var i = 0; i < CityLights.Length; i++)
        {
            var light = CityLightBounds(i);
            var (_, _, rx, ry) = CityLights[i];
            var pixels = new Color[light.Width * light.Height];
            artwork.GetData(0, light, pixels, 0, pixels.Length);
            var surroundings = Vector3.Zero;
            var weightSum = 0f;
            for (var y = 0; y < light.Height; y++)
                for (var x = 0; x < light.Width; x++)
                {
                    var distance = new Vector2((x - rx) / (float)rx, (y - ry) / (float)ry).LengthSquared();
                    if (distance < 0.72f || distance > 1.5f)
                    {
                        continue;
                    }

                    var pixel = pixels[y * light.Width + x];
                    var weight = 1f / (16f + Math.Max(0, pixel.R - pixel.B));
                    surroundings += pixel.ToVector3() * weight;
                    weightSum += weight;
                }

            var unlit = new Color(surroundings / weightSum);
            var hasPaintedLight = false;
            for (var y = ry - 2; y <= ry + 2; y++)
                for (var x = rx - 2; x <= rx + 2; x++)
                {
                    var pixel = pixels[y * light.Width + x];
                    hasPaintedLight |= pixel.R >= 174 && pixel.G >= 113 &&
                        pixel.R - pixel.B >= 24 && pixel.R - unlit.R >= 20;
                }
            if (!hasPaintedLight)
            {
                continue;
            }

            paintedLights.Add(i);
            var tileX = i % LightAtlasColumns * LightCellWidth;
            var tileY = i / LightAtlasColumns * LightCellHeight;
            for (var y = 0; y < light.Height; y++)
                for (var x = 0; x < light.Width; x++)
                {
                    var pixel = pixels[y * light.Width + x];
                    var distance = new Vector2((x - rx) / (float)rx, (y - ry) / (float)ry).LengthSquared();
                    var edge = Math.Clamp((1f - distance) / 0.42f, 0, 1);
                    var warmth = Math.Clamp((pixel.R - pixel.B - 4) / 32f, 0, 1);
                    var brightness = Math.Clamp((pixel.R - unlit.R + 3) / 36f, 0, 1);
                    atlas[(tileY + y) * width + tileX + x] = unlit * (edge * warmth * brightness);
                }
        }

        flickeringLights = paintedLights.ToArray();
        new Random(112).Shuffle(flickeringLights);
        return TextureFactory.FromPixels(artwork.GraphicsDevice, width, height, atlas);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        leaf.Dispose();
        gillMasks.Dispose();
        treeMask.Dispose();
        atmosphere.Dispose();
        forestFog.Dispose();
        cityLightsOff.Dispose();
    }
}
