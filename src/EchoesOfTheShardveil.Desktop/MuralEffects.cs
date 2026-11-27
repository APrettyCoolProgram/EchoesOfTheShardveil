// 261127_code
// 261127_documentation

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EchoesOfTheShardveil.Desktop;

/// <summary>Draws animated light in the source mural's pixel coordinates.</summary>
/// <param name="spriteBatch">A sprite batch that has not begun when <see cref="Draw"/> is called.</param>
/// <param name="glowTexture">The sprite from <see cref="CreateGlow"/>.</param>
/// <param name="artworkMask">The atlas from <see cref="CreateArtworkMask"/>.</param>
/// <param name="mural">The painting.</param>
/// <param name="muralRasterizer">A rasterizer state with scissoring enabled.</param>
internal sealed class MuralEffects(SpriteBatch spriteBatch, Texture2D glowTexture, Texture2D artworkMask, Texture2D mural,
    RasterizerState muralRasterizer)
{
    // The mask atlas covers the top of the mural pixel for pixel, with extra rows below for effects that need their own.
    private const int MaskWidth = 1532;
    private const int MaskHeight = 875;
    private const int AtlasHeight = 1227;
    private static readonly (Rectangle Pane, float Interval, float Offset)[] Windows =
    [
        (new(683, 144, 7, 6), 11.2f, 3.1f),
        (new(683, 151, 6, 6), 8.6f, 1.7f),
        (new(714, 151, 9, 9), 9.3f, 4.2f),
        (new(728, 119, 9, 8), 12.4f, 7.1f),
        (new(706, 173, 11, 34), 7.5f, 7.4f),
        (new(720, 170, 11, 33), 9.8f, 5.3f),
        (new(747, 185, 7, 19), 13.1f, 8.6f),
        (new(974, 54, 5, 5), 9.7f, 2.1f),
        (new(975, 60, 5, 5), 10.8f, 8.4f),
        (new(982, 62, 5, 5), 7.6f, 4.7f),
        (new(975, 71, 5, 5), 11.6f, 6.2f),
        (new(981, 67, 5, 5), 8.3f, 2.8f),
        (new(973, 96, 6, 5), 12.1f, 8.9f),
        (new(981, 103, 6, 6), 9.8f, 3.6f),
        (new(1136, 180, 6, 6), 9.2f, 5.8f),
        (new(1143, 181, 6, 6), 11.3f, 3.2f),
        (new(1155, 215, 7, 6), 8.4f, 5.5f),
        (new(941, 129, 6, 6), 10.5f, 1.9f),
        (new(988, 149, 6, 5), 9.4f, 7.2f),
    ];

    private static readonly Rectangle DragonFire = new(274, 268, 131, 123);
    private static readonly Rectangle CastleBanner = new(2, 417, 52, 97);
    private static readonly Rectangle WizardMagic = new(237, 400, 135, 112);
    private static readonly Vector2 WizardStaffHead = new(321, 473);
    private static readonly Rectangle HeroSwirls = new(846, 169, 132, 140);
    private static readonly Vector2[] BackpackLeftArc =
        [new(1153, 350), new(1158, 331), new(1169, 315), new(1184, 307)];
    private static readonly Vector2[] BackpackRightArc =
        [new(1192, 348), new(1198, 328), new(1213, 313), new(1231, 306), new(1244, 294)];
    private static readonly Rectangle CanisterBubbleMask = new(656, 876, 24, 24);

    private static readonly Vector2[] HandUpperArc =
        [new(1025, 519), new(1015, 512), new(1018, 500), new(1026, 491), new(1042, 487)];
    private static readonly Vector2[] HandOuterArc =
        [new(1031, 525), new(1018, 529), new(1005, 524), new(997, 517)];

    private static readonly Rectangle VampireCape = new(603, 270, 242, 350);
    private static readonly Rectangle HeroCape = new(827, 310, 138, 111);
    private static readonly Rectangle KnightUpperCape = new(0, 503, 142, 161);
    private static readonly Rectangle KnightLowerCape = new(0, 647, 146, 138);

    private static readonly Rectangle WizardRobe = new(203, 535, 179, 225);
    private static readonly Rectangle VillainCape = new(1132, 443, 208, 199);
    private static readonly Rectangle WerewolfFur = new(385, 343, 206, 162);

    // These effects overlap in mural coordinates. Store their masks in padded, separate slots.
    private static readonly Point VampireCapeMaskOrigin = new(1, 876);
    private static readonly Point HeroCapeMaskOrigin = new(245, 876);
    private static readonly Point WerewolfFurMaskOrigin = new(385, 876);

    private static readonly (Rectangle Region, float Angle, float Offset)[] RuinsFog =
    [
        (new(410, 708, 178, 62), -0.24f, 0.12f),
        (new(603, 638, 107, 77), -0.32f, 0.64f),
        (new(589, 775, 219, 75), -0.30f, 0.39f),
        (new(717, 714, 129, 60), -0.42f, 0.83f),
    ];

    private static readonly (Rectangle Region, float Angle, float Offset)[] MoonClouds =
    [
        (new(553, 45, 152, 33), -0.20f, 0.67f),
        (new(502, 86, 102, 39), -0.34f, 0.21f),
        (new(576, 133, 107, 33), -0.08f, 0.42f),
        (new(553, 169, 103, 31), -0.18f, 0.87f),
    ];

    private static readonly Rectangle Galaxy = new(1300, 0, 232, 215);
    private static readonly Vector2 GalaxyCenter = new(1416, 105);
    // Store the feathered rings in unused parts of the artwork atlas.
    private static readonly (Point AtlasOrigin, float Radius, float Width, float AngularSpeed)[] GalaxyRings =
    [
        (new(0, 0), 18f, 31f, 0.078f),
        (new(232, 0), 54f, 37f, 0.052f),
        (new(0, 215), 86f, 35f, 0.033f),
    ];

    /// <summary>Builds a mask from the painted windows, energy trails, flame, fabric, and fog.</summary>
    /// <param name="mural">The painting the masks are cut from.</param>
    /// <returns>The mask atlas, which the caller must dispose.</returns>
    public static Texture2D CreateArtworkMask(Texture2D mural)
    {
        var source = new Color[MaskWidth * MaskHeight];
        mural.GetData(0, new Rectangle(0, 0, MaskWidth, MaskHeight), source, 0, source.Length);
        var mask = new Color[MaskWidth * AtlasHeight];
        foreach (var (pane, _, _) in Windows)
        {
            MaskPixels(pane, source, mask, pixel =>
                MathHelper.Clamp((pixel.R - pixel.B + 4) / 49f, 0f, 1f) *
                MathHelper.Clamp((pixel.R - 114) / 85f, 0f, 1f));
        }

        for (var y = DragonFire.Top; y < DragonFire.Bottom; y++)
        {
            var distance = y - DragonFire.Top;
            var center = (int)(286 + 0.79f * distance);
            var radius = (int)(9 + 0.34f * distance);
            var left = Math.Max(DragonFire.Left, center - radius);
            var right = Math.Min(DragonFire.Right, center + radius);
            var feather = 1f - MathHelper.SmoothStep(0f, 1f, Math.Max(0f, (distance - 88) / 34f));
            for (var x = left; x < right; x++)
            {
                var index = MaskIndex(x, y);
                var pixel = source[index];
                var edge = MathHelper.Clamp((radius - Math.Abs(x - center)) / 6f, 0f, 1f);
                var heat = MathHelper.Clamp((pixel.R - 174) / 69f, 0f, 1f)
                    * MathHelper.Clamp((pixel.G - 78) / 102f, 0f, 1f);
                mask[index] = pixel * (heat * edge * feather);
            }
        }

        for (var y = WizardMagic.Top; y < WizardMagic.Bottom; y++)
        {
            for (var x = WizardMagic.Left; x < WizardMagic.Right; x++)
            {
                var index = MaskIndex(x, y);
                var pixel = source[index];
                var offset = new Vector2((x - 306) / 65f, (y - 456) / 56f);
                var edge = MathHelper.Clamp((1f - offset.LengthSquared()) * 2.1f, 0f, 1f);
                var blue = MathHelper.Clamp((pixel.B - pixel.R - 16f) / 50f, 0f, 1f);
                var light = MathHelper.Clamp((pixel.B - 135f) / 90f, 0f, 1f);
                mask[index] = pixel * (edge * blue * light);
            }
        }

        MaskPixels(HeroSwirls, source, mask, pixel =>
            MathHelper.Clamp((pixel.G - 130) / 75f, 0f, 1f) *
            MathHelper.Clamp((pixel.R - pixel.B - 23) / 53f, 0f, 1f));
        MaskCloth(VampireCape, source, mask,
            [new(785, 270), new(830, 293), new(836, 396), new(829, 480),
             new(787, 520), new(699, 544), new(632, 618), new(607, 568),
             new(615, 483), new(732, 429), new(753, 386), new(730, 342)],
            RedCloth, anchorX: 790, anchorY: 322, maskOrigin: VampireCapeMaskOrigin);
        MaskCloth(HeroCape, source, mask,
            [new(949, 310), new(962, 326), new(945, 354), new(920, 368),
             new(899, 397), new(878, 403), new(859, 421), new(827, 421),
             new(846, 377), new(869, 366), new(903, 345)],
            GoldCloth, anchorX: 955, anchorY: 326, maskOrigin: HeroCapeMaskOrigin);
        MaskCloth(KnightUpperCape, source, mask,
            [new(0, 535), new(47, 539), new(94, 513), new(141, 503),
             new(103, 541), new(75, 605), new(0, 664)],
            RedCloth, anchorX: 131, anchorY: 508);
        MaskCloth(KnightLowerCape, source, mask,
            [new(0, 691), new(42, 683), new(108, 658), new(145, 647),
             new(104, 700), new(81, 741), new(39, 783), new(0, 785)],
            RedCloth, anchorX: 138, anchorY: 652);

        MaskCloth(WizardRobe, source, mask,
            [new(297, 551), new(325, 535), new(319, 602), new(300, 603),
             new(318, 622), new(366, 648), new(381, 682), new(350, 666),
             new(320, 649), new(284, 640), new(245, 634), new(203, 624),
             new(220, 592)],
            pixel => MathHelper.Clamp((pixel.B - pixel.R + 4) / 34f, 0f, 1f),
            anchorX: 288, anchorY: 560);

        MaskCloth(VillainCape, source, mask,
            [new(1133, 475), new(1181, 503), new(1219, 494), new(1246, 463),
             new(1288, 443), new(1339, 450), new(1339, 548), new(1310, 581),
             new(1321, 631), new(1282, 641), new(1228, 601), new(1174, 562),
             new(1142, 523)],
            pixel => MathHelper.Clamp((Math.Min(pixel.R, pixel.B) - pixel.G - 4) / 35f, 0f, 1f),
            anchorX: 1138, anchorY: 487);

        MaskCloth(WerewolfFur, source, mask,
            [new(386, 433), new(408, 404), new(407, 391), new(440, 374),
             new(467, 362), new(491, 357), new(510, 344), new(542, 349),
             new(569, 357), new(590, 363), new(570, 385), new(551, 400),
             new(517, 402), new(496, 418), new(477, 416), new(449, 433),
             new(428, 447), new(410, 474), new(386, 504), new(394, 466)],
            pixel => MathHelper.Clamp((pixel.B - pixel.R + 13) / 36f, 0f, 1f),
            anchorX: 527, anchorY: 412, maskOrigin: WerewolfFurMaskOrigin);

        foreach (var (region, angle, _) in RuinsFog)
        {
            MaskMist(region, angle, source, mask, moonCloud: false);
        }

        foreach (var (region, angle, _) in MoonClouds)
        {
            MaskMist(region, angle, source, mask, moonCloud: true);
        }

        for (var y = Galaxy.Top; y < Galaxy.Bottom; y++)
        {
            for (var x = Galaxy.Left; x < Galaxy.Right; x++)
            {
                var index = MaskIndex(x, y);
                var pixel = source[index];
                var distance = Vector2.Distance(new Vector2(x, y), GalaxyCenter);
                var edge = 1f - MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp((distance - 66f) / 37f, 0f, 1f));
                var purple = MathHelper.Clamp((pixel.R - pixel.G - 2f) / 24f, 0f, 1f);
                var coverage = edge * purple;
                mask[index] = pixel * coverage;

                foreach (var (atlasOrigin, radius, width, _) in GalaxyRings)
                {
                    var ring = 1f - MathHelper.SmoothStep(0f, 1f,
                        MathHelper.Clamp(Math.Abs(distance - radius) / width, 0f, 1f));
                    var atlasX = atlasOrigin.X + x - Galaxy.X;
                    var atlasY = atlasOrigin.Y + y - Galaxy.Y;
                    mask[MaskIndex(atlasX, atlasY)] = pixel * (coverage * ring);
                }
            }
        }

        for (var y = 0; y < CanisterBubbleMask.Height; y++)
            for (var x = 0; x < CanisterBubbleMask.Width; x++)
            {
                var point = new Vector2(x + 0.5f, y + 0.5f) / 12f - Vector2.One;
                var radius = point.Length();
                var rim = Math.Clamp(1f - MathF.Abs(radius - 0.73f) / 0.23f, 0, 1);
                var gleam = Math.Clamp(1f - Vector2.Distance(point, new Vector2(-0.36f, -0.43f)) / 0.37f, 0, 1);
                var glass = Math.Clamp((0.78f - radius) * 6f, 0, 1);
                var index = MaskIndex(CanisterBubbleMask.X + x, CanisterBubbleMask.Y + y);
                mask[index] = Color.Lerp(new Color(33, 91, 31), new Color(224, 255, 152), Math.Max(rim * 0.85f, gleam)) *
                    Math.Max(glass * 0.39f, rim * 0.80f);
            }

        return TextureFactory.FromPixels(mural.GraphicsDevice, MaskWidth, AtlasHeight, mask);
    }

    /// <summary>Converts mural coordinates to an index into the mask atlas.</summary>
    /// <param name="x">The horizontal position.</param>
    /// <param name="y">The vertical position.</param>
    /// <returns>The pixel index.</returns>
    private static int MaskIndex(int x, int y) => y * MaskWidth + x;

    /// <summary>Copies the bluish haze of a tilted ellipse into the mask.</summary>
    /// <param name="region">The area around the ellipse.</param>
    /// <param name="angle">The tilt in radians.</param>
    /// <param name="source">The mural pixels.</param>
    /// <param name="mask">The atlas being built.</param>
    /// <param name="moonCloud">
    /// Whether to select the brighter clouds near the moon rather than the fog in the ruins.
    /// </param>
    private static void MaskMist(Rectangle region, float angle, Color[] source, Color[] mask, bool moonCloud)
    {
        var cosine = MathF.Cos(angle);
        var sine = MathF.Sin(angle);
        for (var y = region.Top; y < region.Bottom; y++)
        {
            for (var x = region.Left; x < region.Right; x++)
            {
                var dx = x - region.Center.X;
                var dy = y - region.Center.Y;
                var major = (dx * cosine + dy * sine) * 2f / region.Width;
                var minor = (-dx * sine + dy * cosine) * 2f / region.Height;
                var edge = 1f - MathHelper.Clamp(major * major + minor * minor, 0f, 1f);
                var index = MaskIndex(x, y);
                var pixel = source[index];
                var blue = MathHelper.Clamp((pixel.B - pixel.R - 5) / 25f, 0f, 1f);
                var mist = moonCloud
                    ? MathHelper.Clamp((pixel.B - 69) / 41f, 0f, 1f)
                      * MathHelper.Clamp((225 - pixel.R) / 42f, 0f, 1f)
                    : MathHelper.Clamp((pixel.B - 93) / 60f, 0f, 1f);
                var feather = moonCloud ? edge : edge * MathF.Sqrt(edge);
                mask[index] = pixel * (feather * mist * blue);
            }
        }
    }

    /// <summary>Writes a black mask for the pixels of a region, with opacity chosen from each source color.</summary>
    /// <param name="region">The area.</param>
    /// <param name="source">The mural pixels.</param>
    /// <param name="mask">The atlas being built.</param>
    /// <param name="opacity">Gives the opacity for a source color.</param>
    private static void MaskPixels(Rectangle region, Color[] source, Color[] mask, Func<Color, float> opacity)
    {
        for (var y = region.Top; y < region.Bottom; y++)
        {
            for (var x = region.Left; x < region.Right; x++)
            {
                var index = MaskIndex(x, y);
                mask[index] = Color.Black * opacity(source[index]);
            }
        }
    }

    /// <summary>Copies cloth or fur inside an outline into the mask, fading it out where it is held.</summary>
    /// <param name="region">The area around the outline.</param>
    /// <param name="source">The mural pixels.</param>
    /// <param name="mask">The atlas being built.</param>
    /// <param name="outline">The corners of the cloth in mural coordinates.</param>
    /// <param name="coverage">Gives how strongly a color belongs to the cloth.</param>
    /// <param name="anchorX">The horizontal position that stays still.</param>
    /// <param name="anchorY">The vertical position that stays still.</param>
    /// <param name="maskOrigin">Where to store the mask in the atlas, if not over its own region.</param>
    private static void MaskCloth(Rectangle region, Color[] source, Color[] mask, Point[] outline,
        Func<Color, float> coverage, int anchorX, int anchorY, Point? maskOrigin = null)
    {
        var origin = maskOrigin ?? region.Location;
        var corners = Array.ConvertAll(outline, corner => corner.ToVector2());
        for (var y = region.Top; y < region.Bottom; y++)
        {
            for (var x = region.Left; x < region.Right; x++)
            {
                if (!Geometry.IsInPolygon(new Vector2(x, y), corners))
                {
                    continue;
                }

                var index = MaskIndex(x, y);
                var pixel = source[index];
                var distance = Vector2.Distance(new Vector2(x, y), new Vector2(anchorX, anchorY));
                var anchored = MathHelper.Clamp((distance - 8f) / 46f, 0f, 1f);
                if (region.X == 0)
                {
                    anchored *= MathHelper.SmoothStep(0, 1,
                        Math.Clamp(Math.Min(y - region.Top, region.Bottom - y) / 16f, 0, 1));
                    anchored *= CapeEdgeCoverage(new Vector2(x, y), corners);
                }
                var maskIndex = MaskIndex(origin.X + x - region.X, origin.Y + y - region.Y);
                mask[maskIndex] = pixel * (coverage(pixel) * anchored);
            }
        }
    }

    /// <summary>Rates how much a color looks like the gold cloth.</summary>
    /// <param name="pixel">The color.</param>
    /// <returns>From 0 to 1.</returns>
    private static float GoldCloth(Color pixel) =>
        MathHelper.Clamp((pixel.G - pixel.B - 25) / 52f, 0f, 1f)
        * MathHelper.Clamp((pixel.R - pixel.G - 10) / 52f, 0f, 1f);

    /// <summary>Rates how much a color looks like the red cloth.</summary>
    /// <param name="pixel">The color.</param>
    /// <returns>From 0 to 1.</returns>
    private static float RedCloth(Color pixel) =>
        MathHelper.Clamp((pixel.R - Math.Max(pixel.G, pixel.B) - 6) / 41f, 0f, 1f);

    /// <summary>Softens a cloth mask near its outline.</summary>
    /// <param name="point">A point inside the outline.</param>
    /// <param name="outline">The corners of the cloth.</param>
    /// <returns>From 0 on the edge to 1 well inside.</returns>
    private static float CapeEdgeCoverage(Vector2 point, Vector2[] outline)
    {
        const float feather = 7f;
        var distanceSquared = feather * feather;
        for (var i = 0; i < outline.Length; i++)
        {
            var start = outline[i];
            var end = outline[(i + 1) % outline.Length];
            // An edge along the left border of the painting is not an edge of the cloth.
            if (start.X == 0 && end.X == 0)
            {
                continue;
            }

            distanceSquared = Math.Min(distanceSquared, Geometry.DistanceSquaredToSegment(point, start, end));
        }

        return MathHelper.SmoothStep(0, 1, MathF.Sqrt(distanceSquared) / feather);
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

    /// <summary>Draws the effects using elapsed game time and the mural's screen transform.</summary>
    /// <param name="time">Seconds of animation.</param>
    /// <param name="muralTransform">The transform from mural coordinates to the screen.</param>
    public void Draw(float time, Matrix muralTransform)
    {
        DrawMaskedArtwork(time, muralTransform);

        spriteBatch.Begin(blendState: BlendState.Additive, samplerState: SamplerState.LinearClamp,
            rasterizerState: muralRasterizer, transformMatrix: muralTransform);

        DrawClouds(time);
        DrawLightning(time, BackpackLeftArc, 1.53f, 0.18f);
        DrawLightning(time, BackpackRightArc, 1.91f, 0.92f);
        DrawLightning(time, HandUpperArc, 1.74f, 0.53f);
        DrawLightning(time, HandOuterArc, 2.16f, 1.13f);

        var gold = new Color(255, 188, 73);
        var blue = new Color(67, 166, 255);
        var red = new Color(255, 61, 99);

        DrawEmbers(time, gold);

        DrawWizardMagic(time);

        DrawGlow(new Vector2(1566, 609), 148, 28, blue, EnginePulse(time), 0.68f);
        DrawGlow(new Vector2(1631, 616), 120, 25, blue, EnginePulse(time + 0.15f), 0.49f);
        DrawGlow(new Vector2(1372, 587), 63, 12, blue, EnginePulse(time + 0.28f), 0.90f);

        DrawLaser(time, 2.15f, new Vector2(1565, 298), new Vector2(1381, 362), red);
        DrawLaser(time + 0.13f, 2.15f, new Vector2(1562, 307), new Vector2(1480, 351), red);
        DrawLaser(time + 1.1f, 2.7f, new Vector2(1362, 530), new Vector2(1434, 590), red);

        spriteBatch.End();
    }

    /// <summary>Draws the drifting smoke and nebula puffs.</summary>
    /// <param name="time">Seconds of animation.</param>
    private void DrawClouds(float time)
    {
        var smoke = new Color(184, 144, 132);
        var nebula = new Color(114, 127, 201);
        DrawCloudPuff(time, new Vector2(1070, 746), smoke, 4f);
        DrawCloudPuff(time, new Vector2(1225, 833), smoke, 1f);
        DrawCloudPuff(time, new Vector2(1430, 761), nebula, 3f);
    }

    /// <summary>Draws one puff that fades in, drifts and fades out.</summary>
    /// <param name="time">Seconds of animation.</param>
    /// <param name="center">The middle of its path.</param>
    /// <param name="color">The tint.</param>
    /// <param name="offset">Staggers this puff against the others.</param>
    private void DrawCloudPuff(float time, Vector2 center, Color color, float offset)
    {
        const float width = 270;
        const float height = 76;
        var phase = (time / 15f + offset / 5f) % 1f;
        var opacity = 0.11f * MathF.Sin(MathF.PI * phase);
        var drift = new Vector2(102f * (phase - 0.5f), -25f * phase);
        DrawGlow(center + drift, width, height, color, opacity, -0.18f);
        DrawGlow(center + drift + new Vector2(41, -20), width * 0.57f, height * 0.66f, color, opacity * 0.7f, -0.35f);
    }

    /// <summary>Draws the effects that move or dim copies of the painting.</summary>
    /// <param name="time">Seconds of animation.</param>
    /// <param name="muralTransform">The mural's screen transform.</param>
    private void DrawMaskedArtwork(float time, Matrix muralTransform)
    {
        spriteBatch.Begin(samplerState: SamplerState.LinearClamp, rasterizerState: muralRasterizer, transformMatrix: muralTransform);
        DrawDriftingMist(RuinsFog, time, 8.5f, 103f, 0.96f);
        DrawDriftingMist(MoonClouds, time, 9.5f, 108f, 1f);
        DrawGalaxy(time);
        DrawBanner(time);
        DrawDragonFire(time);
        DrawWavingCloth(VampireCape, time, 1.9f, 0.1f, 10f, VampireCapeMaskOrigin);
        DrawWavingCloth(HeroCape, time, 3.5f, 1.8f, 8f, HeroCapeMaskOrigin);
        DrawWavingCloth(KnightUpperCape, time, 2.05f, 2.4f, 8f);
        DrawWavingCloth(KnightLowerCape, time, 2.05f, 2.4f, 10f);
        DrawWavingCloth(WizardRobe, time, 2.0f, 3.5f, 7.5f);
        DrawWavingCloth(VillainCape, time, 2.6f, 1.1f, 11f);
        DrawWavingCloth(WerewolfFur, time, 3.4f, 2.1f, 6f, WerewolfFurMaskOrigin);
        DrawCanisterBubbles(time);

        foreach (var (pane, interval, offset) in Windows)
        {
            var phase = (time + offset) % interval / 0.86f;
            if (phase >= 1f)
            {
                continue;
            }

            var fade = MathF.Sin(MathF.PI * phase);
            var flutter = 0.75f + 0.25f * MathF.Sin(phase * MathHelper.TwoPi * 2.4f);
            DimPaintedPixels(pane, 0.82f * fade * flutter);
        }

        for (var x = HeroSwirls.Left; x < HeroSwirls.Right; x += 2)
        {
            var phase = (x - HeroSwirls.Left) * 0.095f - time * 4.2f;
            var brightness = 0.5f + 0.5f * MathF.Sin(phase);
            DimPaintedPixels(new Rectangle(x, HeroSwirls.Y, 2, HeroSwirls.Height), 0.43f * brightness);
        }

        spriteBatch.End();
    }

    /// <summary>Draws bubbles rising in the backpack canister.</summary>
    /// <param name="time">Seconds of animation.</param>
    private void DrawCanisterBubbles(float time)
    {
        var bottom = new Vector2(1185f, 375.5f);
        var top = new Vector2(1195f, 361.8f);
        var across = new Vector2(0.81f, 0.59f);
        var origin = new Vector2(CanisterBubbleMask.Width / 2f, CanisterBubbleMask.Height / 2f);
        for (var i = 0; i < 8; i++)
        {
            var clock = time * (0.31f + i % 3 * 0.09f) + i * 0.618f;
            var age = clock - MathF.Floor(clock);
            var rise = age * (0.72f + age * 0.28f);
            var wobble = (i % 4 - 1.5f) * 1.65f + 0.90f * MathF.Sin(age * 8f + i * 2.4f);
            var position = Vector2.Lerp(bottom, top, rise) + across * wobble;
            var diameter = (2.0f + i % 4 * 0.69f) * (0.63f + 0.5f * age);
            var opacity = Math.Clamp(age * 8f, 0, 1) * Math.Clamp((1 - age) * 12f, 0, 1);
            spriteBatch.Draw(artworkMask, position, CanisterBubbleMask, Color.White * opacity, 0f, origin,
                diameter / CanisterBubbleMask.Width, SpriteEffects.None, 0f);
        }
    }

    /// <summary>Slides masked haze layers along their angle, fading in and out.</summary>
    /// <param name="layers">The areas, directions and phase offsets.</param>
    /// <param name="time">Seconds of animation.</param>
    /// <param name="period">Seconds for one pass.</param>
    /// <param name="distance">How far a layer travels.</param>
    /// <param name="strength">The peak opacity.</param>
    private void DrawDriftingMist((Rectangle Region, float Angle, float Offset)[] layers,
        float time, float period, float distance, float strength)
    {
        foreach (var (region, angle, offset) in layers)
        {
            var phase = (time / period + offset) % 1f;
            var opacity = strength * MathF.Sin(MathF.PI * phase);
            var travel = distance * (phase - 0.5f);
            var position = new Vector2(region.X + travel * MathF.Cos(angle),
                region.Y + travel * MathF.Sin(angle));
            spriteBatch.Draw(artworkMask, position, region, Color.White * opacity);
        }
    }

    /// <summary>Rotates the galaxy's feathered rings at different speeds.</summary>
    /// <param name="time">Seconds of animation.</param>
    private void DrawGalaxy(float time)
    {
        var origin = GalaxyCenter - new Vector2(Galaxy.X, Galaxy.Y);
        spriteBatch.Draw(artworkMask, GalaxyCenter, Galaxy,
            new Color(0.24f, 0.28f, 0.49f) * 0.94f, 0f, origin, 1f, SpriteEffects.None, 0f);

        foreach (var (atlasOrigin, _, _, angularSpeed) in GalaxyRings)
        {
            var ring = new Rectangle(atlasOrigin.X, atlasOrigin.Y, Galaxy.Width, Galaxy.Height);
            var rotation = (time * angularSpeed) % MathHelper.TwoPi;
            spriteBatch.Draw(artworkMask, GalaxyCenter, ring, Color.White, rotation, origin,
                1f, SpriteEffects.None, 0f);
        }
    }

    /// <summary>Ripples the dragon's fire row by row.</summary>
    /// <param name="time">Seconds of animation.</param>
    private void DrawDragonFire(float time)
    {
        for (var y = DragonFire.Top; y < DragonFire.Bottom; y++)
        {
            var distance = y - DragonFire.Top;
            var turbulence = MathHelper.Clamp(distance / 65f, 0f, 1f);
            var wave = MathF.Sin(distance * 0.115f - time * 12f);
            var curl = MathF.Sin(distance * 0.21f - time * 17f + 1.6f);
            var shift = turbulence * (2.8f * wave + 1.4f * curl);
            var brightness = 0.87f + 0.11f * turbulence * wave;
            var source = new Rectangle(DragonFire.X, y, DragonFire.Width, 1);
            spriteBatch.Draw(artworkMask, new Vector2(DragonFire.X + shift, y), source,
                new Color(brightness, brightness, brightness) * 0.83f);
        }
    }

    /// <summary>Draws sparks falling from the dragon's fire.</summary>
    /// <param name="time">Seconds of animation.</param>
    /// <param name="gold">The color of a new spark.</param>
    private void DrawEmbers(float time, Color gold)
    {
        var orange = new Color(255, 92, 27);
        for (var i = 0; i < 15; i++)
        {
            var speed = 0.57f + (i % 5) * 0.10f;
            var phase = (time * speed + i * 0.618f) % 1f;
            var scatter = MathF.Sin(i * 3.7f);
            var position = new Vector2(
                295 + 73 * phase + 59 * scatter * phase * phase,
                283 + 105 * phase - 60 * phase * phase + 22 * MathF.Cos(i * 4.1f) * phase);
            var fade = MathF.Sin(MathF.PI * phase) * (1f - 0.55f * phase);
            var color = Color.Lerp(gold, orange, phase);
            DrawGlow(position, 4 + i % 4, 2.4f, color, 0.64f * fade, 0.75f - scatter * 0.52f);
        }
    }

    /// <summary>Sways and pulses the glow around the wizard's staff.</summary>
    /// <param name="time">Seconds of animation.</param>
    private void DrawWizardMagic(float time)
    {
        var origin = WizardStaffHead - new Vector2(WizardMagic.X, WizardMagic.Y);
        var pulse = 0.5f + 0.5f * MathF.Sin(time * 3.4f);
        var rotation = 0.21f * MathF.Sin(time * 2.1f) + 0.09f * MathF.Sin(time * 4.2f);
        var scale = 0.95f + 0.10f * pulse;

        var blue = new Color(121, 151, 255) { A = (byte)(32f + 48f * pulse) };
        spriteBatch.Draw(artworkMask, WizardStaffHead, WizardMagic, blue, rotation, origin,
            scale, SpriteEffects.None, 0f);
        var violet = new Color(178, 158, 255) { A = (byte)(23f + 27f * (1f - pulse)) };
        spriteBatch.Draw(artworkMask, WizardStaffHead, WizardMagic, violet, -rotation * 0.75f, origin,
            1.03f, SpriteEffects.None, 0f);
    }

    /// <summary>Sends a wave down a masked cloth, row by row.</summary>
    /// <param name="region">The cloth's area in the mural.</param>
    /// <param name="time">Seconds of animation.</param>
    /// <param name="rate">The wave speed.</param>
    /// <param name="offset">The wave's starting phase.</param>
    /// <param name="amplitude">The sideways movement in mural pixels.</param>
    /// <param name="maskOrigin">Where the mask is stored in the atlas, if not over its own region.</param>
    private void DrawWavingCloth(Rectangle region, float time, float rate, float offset, float amplitude,
        Point? maskOrigin = null)
    {
        var origin = maskOrigin ?? region.Location;
        var cycle = time * rate + offset;
        var edgeCloth = region.X == 0;
        var rowTop = (float)region.Top;
        for (var y = region.Top; y < region.Bottom; y++)
        {
            var distance = (y - region.Top) / (float)region.Height;
            var phase = cycle - (edgeCloth ? (y - KnightUpperCape.Y) * 0.036f : distance * 9f);
            var wave = 0.78f * MathF.Sin(phase) + 0.22f * MathF.Sin(phase * 1.7f + 0.9f);
            var shift = amplitude * wave * (0.45f + 0.55f * distance);
            var brightness = 0.85f + 0.15f * MathF.Sin(phase + 0.8f);

            var nextDistance = (y + 1 - region.Top) / (float)region.Height;
            var lift = amplitude * 0.38f * MathF.Sin(cycle - nextDistance * 7f)
                * MathF.Sin(nextDistance * MathF.PI);
            var rowBottom = y + 1 + lift;
            var source = new Rectangle(origin.X, origin.Y + y - region.Y, region.Width, 1);
            // The knight's cloth runs out of the painting. Let it stretch past that edge
            // while holding the shoulder end in place.
            var x = edgeCloth ? Math.Min(0, shift - amplitude) : region.X + shift;
            var widthScale = edgeCloth ? (region.Width - x) / region.Width : 1f;
            var tint = new Color(brightness, brightness, brightness) * 0.87f;
            var scale = new Vector2(widthScale, rowBottom - rowTop);
            var overhang = (int)MathF.Ceiling(Math.Max(0, -x) / widthScale);
            for (var column = 0; column < overhang; column += 2)
            {
                var width = Math.Min(2, overhang - column);
                var amount = MathHelper.SmoothStep(0, 1, (column + width / 2f) / overhang);
                var tip = new Rectangle(source.X + column, source.Y, width, 1);
                spriteBatch.Draw(artworkMask, new Vector2(x + column * widthScale, rowTop), tip,
                    tint * amount, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
            }
            source.X += overhang;
            source.Width -= overhang;
            spriteBatch.Draw(artworkMask, new Vector2(x + overhang * widthScale, rowTop), source,
                tint, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
            rowTop = rowBottom;
        }
    }

    /// <summary>Waves the castle banner, keeping its top and bottom still.</summary>
    /// <param name="time">Seconds of animation.</param>
    private void DrawBanner(float time)
    {
        var halfWidth = CastleBanner.Width / 2;
        for (var y = CastleBanner.Top; y < CastleBanner.Bottom; y++)
        {
            var distance = (y - CastleBanner.Top) / (float)(CastleBanner.Height - 1);
            var falloff = distance * MathHelper.Clamp((1f - distance) * 12f, 0f, 1f);
            var shift = falloff * (5.2f * MathF.Sin(2.3f * time - distance * 6f)
                + 1.9f * MathF.Sin(4.1f * time - distance * 9f));
            var left = new Rectangle(CastleBanner.X, y, halfWidth, 1);
            var right = new Rectangle(left.Right, y, halfWidth, 1);
            spriteBatch.Draw(mural, new Vector2(left.X, y), left, Color.White, 0f, Vector2.Zero,
                new Vector2(1f + shift / halfWidth, 1f), SpriteEffects.None, 0f);
            spriteBatch.Draw(mural, new Vector2(right.X + shift, y), right, Color.White, 0f, Vector2.Zero,
                new Vector2(1f - shift / halfWidth, 1f), SpriteEffects.None, 0f);
        }
    }

    /// <summary>Darkens the painted light selected by the mask.</summary>
    /// <param name="region">The area.</param>
    /// <param name="opacity">How much to darken.</param>
    private void DimPaintedPixels(Rectangle region, float opacity)
    {
        spriteBatch.Draw(artworkMask, region, region, Color.White * opacity);
    }

    /// <summary>Draws a brief jagged bolt along a path at regular intervals.</summary>
    /// <param name="time">Seconds of animation.</param>
    /// <param name="path">The points the bolt passes through.</param>
    /// <param name="interval">Seconds between bolts.</param>
    /// <param name="offset">Staggers this bolt against the others.</param>
    private void DrawLightning(float time, Vector2[] path, float interval, float offset)
    {
        const float burstLength = 0.23f;
        var elapsed = time + offset;
        var age = elapsed % interval;
        if (age >= burstLength)
        {
            return;
        }

        var frame = (int)(age * 28f);
        var strike = (int)(elapsed / interval);
        var intensity = (1f - age / burstLength) * (frame % 3 == 1 ? 0.48f : 0.92f);
        var previous = path[0];
        for (var i = 1; i < path.Length; i++)
        {
            var point = path[i];
            var direction = point - path[i - 1];
            var normal = Vector2.Normalize(new Vector2(-direction.Y, direction.X));
            var jitter = MathF.Sin(strike * 4.7f + frame * 2.3f + i * 5.2f);
            var kink = (path[i - 1] + point) * 0.5f + normal * (5.5f * jitter);
            DrawLightningSegment(previous, kink, intensity);
            DrawLightningSegment(kink, point, intensity);
            previous = point;
        }
    }

    /// <summary>Draws one straight piece of a bolt with its glow.</summary>
    /// <param name="start">One end.</param>
    /// <param name="end">The other end.</param>
    /// <param name="intensity">The brightness.</param>
    private void DrawLightningSegment(Vector2 start, Vector2 end, float intensity)
    {
        var direction = end - start;
        var angle = MathF.Atan2(direction.Y, direction.X);
        var length = direction.Length();
        DrawGlow((start + end) * 0.5f, length + 12f, 11f, new Color(102, 255, 59), intensity * 0.44f, angle);
        var core = new Color(207, 255, 159) { A = (byte)(intensity * 255f) };
        DrawBeamCore(start, core, angle, length + 0.8f, 1.55f);
    }

    /// <summary>Gets the flickering brightness of an engine.</summary>
    /// <param name="time">Seconds of animation.</param>
    /// <returns>The intensity.</returns>
    private static float EnginePulse(float time) =>
        0.21f + 0.07f * MathF.Sin(10 * time) + 0.03f * MathF.Sin(17 * time);

    /// <summary>Fires a laser shot from one point toward another at regular intervals.</summary>
    /// <param name="time">Seconds of animation.</param>
    /// <param name="interval">Seconds between shots.</param>
    /// <param name="start">Where the shot begins.</param>
    /// <param name="end">Where it ends.</param>
    /// <param name="color">The color of its glow.</param>
    private void DrawLaser(float time, float interval, Vector2 start, Vector2 end, Color color)
    {
        var phase = time % interval / 0.43f;
        if (phase >= 1f)
        {
            return;
        }

        var direction = Vector2.Normalize(end - start);
        var angle = MathF.Atan2(direction.Y, direction.X);
        var head = Vector2.Lerp(start, end, phase);
        var envelope = MathF.Min(1f, phase * 10f) * MathF.Min(1f, (1f - phase) * 5f);
        var length = MathHelper.Lerp(34f, 88f, phase);
        var center = head - direction * length * 0.5f;
        DrawGlow(center, length * 1.5f, 23f, color, 0.72f * envelope, angle);
        DrawGlow(center, length, 7f, new Color(255, 139, 127), 0.86f * envelope, angle);
        DrawGlow(head, 23f, 13f, new Color(255, 213, 184), 0.70f * envelope, angle);

        var core = new Color(255, 205, 183) { A = (byte)(0.84f * envelope * 255f) };
        DrawBeamCore(head - direction * length, core, angle, length, 2.3f);
    }

    /// <summary>Draws a solid line by stretching the opaque center pixel of the glow sprite.</summary>
    /// <param name="start">Where the line begins.</param>
    /// <param name="color">The color.</param>
    /// <param name="angle">The direction in radians.</param>
    /// <param name="length">The length.</param>
    /// <param name="thickness">The width of the line.</param>
    private void DrawBeamCore(Vector2 start, Color color, float angle, float length, float thickness)
    {
        var centerPixel = new Rectangle(glowTexture.Width / 2, glowTexture.Height / 2, 1, 1);
        spriteBatch.Draw(glowTexture, start, centerPixel, color, angle, new Vector2(0f, 0.5f),
            new Vector2(length, thickness), SpriteEffects.None, 0f);
    }

    /// <summary>Draws the soft light sprite.</summary>
    /// <param name="center">The middle of the light.</param>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    /// <param name="color">The color.</param>
    /// <param name="intensity">The strength.</param>
    /// <param name="angle">The rotation in radians.</param>
    private void DrawGlow(Vector2 center, float width, float height, Color color, float intensity, float angle = 0)
    {
        color.A = (byte)MathF.Round(MathHelper.Clamp(intensity * 1.32f, 0f, 1f) * 255);
        var origin = new Vector2(glowTexture.Width / 2f, glowTexture.Height / 2f);
        var scale = new Vector2(width / glowTexture.Width, height / glowTexture.Height);
        spriteBatch.Draw(glowTexture, center, null, color, angle, origin, scale, SpriteEffects.None, 0);
    }
}
