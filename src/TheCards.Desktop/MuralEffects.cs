using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TheCards.Desktop;

/// <summary>Draws animated light in the source mural's pixel coordinates.</summary>
internal sealed class MuralEffects(SpriteBatch batch, Texture2D glow, Texture2D artworkMask, Texture2D mural,
    RasterizerState muralRasterizer)
{
    private static readonly Rectangle MaskArea = new(0, 0, 1532, 875);
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
    public static Texture2D CreateArtworkMask(Texture2D mural)
    {
        var area = MaskArea;
        var source = new Color[area.Width * area.Height];
        mural.GetData(0, area, source, 0, source.Length);
        var mask = new Color[area.Width * AtlasHeight];
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
                var index = (y - area.Y) * area.Width + x - area.X;
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
                var index = (y - area.Y) * area.Width + x - area.X;
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
        MaskCape(VampireCape, source, mask,
            [new(785, 270), new(830, 293), new(836, 396), new(829, 480),
             new(787, 520), new(699, 544), new(632, 618), new(607, 568),
             new(615, 483), new(732, 429), new(753, 386), new(730, 342)],
            RedCloth, anchorX: 790, anchorY: 322, maskOrigin: VampireCapeMaskOrigin);
        MaskCape(HeroCape, source, mask,
            [new(949, 310), new(962, 326), new(945, 354), new(920, 368),
             new(899, 397), new(878, 403), new(859, 421), new(827, 421),
             new(846, 377), new(869, 366), new(903, 345)],
            GoldCloth, anchorX: 955, anchorY: 326, maskOrigin: HeroCapeMaskOrigin);
        MaskCape(KnightUpperCape, source, mask,
            [new(0, 535), new(47, 539), new(94, 513), new(141, 503),
             new(103, 541), new(75, 605), new(0, 664)],
            RedCloth, anchorX: 131, anchorY: 508);
        MaskCape(KnightLowerCape, source, mask,
            [new(0, 691), new(42, 683), new(108, 658), new(145, 647),
             new(104, 700), new(81, 741), new(39, 783), new(0, 785)],
            RedCloth, anchorX: 138, anchorY: 652);

        MaskCape(WizardRobe, source, mask,
            [new(297, 551), new(325, 535), new(319, 602), new(300, 603),
             new(318, 622), new(366, 648), new(381, 682), new(350, 666),
             new(320, 649), new(284, 640), new(245, 634), new(203, 624),
             new(220, 592)],
            pixel => MathHelper.Clamp((pixel.B - pixel.R + 4) / 34f, 0f, 1f),
            anchorX: 288, anchorY: 560);

        MaskCape(VillainCape, source, mask,
            [new(1133, 475), new(1181, 503), new(1219, 494), new(1246, 463),
             new(1288, 443), new(1339, 450), new(1339, 548), new(1310, 581),
             new(1321, 631), new(1282, 641), new(1228, 601), new(1174, 562),
             new(1142, 523)],
            pixel => MathHelper.Clamp((Math.Min(pixel.R, pixel.B) - pixel.G - 4) / 35f, 0f, 1f),
            anchorX: 1138, anchorY: 487);

        MaskCape(WerewolfFur, source, mask,
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
                var index = (y - area.Y) * area.Width + x - area.X;
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
                    mask[atlasY * area.Width + atlasX] = pixel * (coverage * ring);
                }
            }
        }

        var texture = new Texture2D(mural.GraphicsDevice, area.Width, AtlasHeight);
        texture.SetData(mask);
        return texture;
    }

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
                var index = (y - MaskArea.Y) * MaskArea.Width + x - MaskArea.X;
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

    private static void MaskPixels(Rectangle region, Color[] source, Color[] mask, Func<Color, float> opacity)
    {
        for (var y = region.Top; y < region.Bottom; y++)
        {
            for (var x = region.Left; x < region.Right; x++)
            {
                var index = (y - MaskArea.Y) * MaskArea.Width + x - MaskArea.X;
                mask[index] = Color.Black * opacity(source[index]);
            }
        }
    }

    private static void MaskCape(Rectangle region, Color[] source, Color[] mask, Point[] outline,
        Func<Color, float> coverage, int anchorX, int anchorY, Point? maskOrigin = null)
    {
        var origin = maskOrigin ?? region.Location;
        for (var y = region.Top; y < region.Bottom; y++)
        {
            for (var x = region.Left; x < region.Right; x++)
            {
                if (!InsideOutline(x, y, outline))
                {
                    continue;
                }

                var index = (y - MaskArea.Y) * MaskArea.Width + x - MaskArea.X;
                var pixel = source[index];
                var distance = Vector2.Distance(new Vector2(x, y), new Vector2(anchorX, anchorY));
                var anchored = MathHelper.Clamp((distance - 8f) / 46f, 0f, 1f);
                var maskIndex = (origin.Y + y - region.Y) * MaskArea.Width + origin.X + x - region.X;
                mask[maskIndex] = pixel * (coverage(pixel) * anchored);
            }
        }
    }

    private static float GoldCloth(Color pixel) =>
        MathHelper.Clamp((pixel.G - pixel.B - 25) / 52f, 0f, 1f)
        * MathHelper.Clamp((pixel.R - pixel.G - 10) / 52f, 0f, 1f);

    private static float RedCloth(Color pixel) =>
        MathHelper.Clamp((pixel.R - Math.Max(pixel.G, pixel.B) - 6) / 41f, 0f, 1f);

    private static bool InsideOutline(int x, int y, Point[] outline)
    {
        var inside = false;
        for (var i = 0; i < outline.Length; i++)
        {
            var start = outline[i];
            var end = outline[(i + 1) % outline.Length];
            if ((start.Y > y) != (end.Y > y)
                && x < start.X + (y - start.Y) * (end.X - start.X) / (float)(end.Y - start.Y))
            {
                inside = !inside;
            }
        }

        return inside;
    }

    /// <summary>Creates a soft light sprite shared by all mural effects.</summary>
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

        var texture = new Texture2D(device, size, size);
        texture.SetData(pixels);
        return texture;
    }

    /// <summary>Draws the effects using elapsed game time and the mural's screen transform.</summary>
    public void Draw(float time, Matrix muralTransform)
    {
        DrawMaskedArtwork(time, muralTransform);

        batch.Begin(blendState: BlendState.Additive, samplerState: SamplerState.LinearClamp,
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

        Light(new Vector2(1566, 609), 148, 28, blue, EnginePulse(time), 0.68f);
        Light(new Vector2(1631, 616), 120, 25, blue, EnginePulse(time + 0.15f), 0.49f);
        Light(new Vector2(1372, 587), 63, 12, blue, EnginePulse(time + 0.28f), 0.90f);

        Laser(time, 2.15f, new Vector2(1565, 298), new Vector2(1381, 362), red);
        Laser(time + 0.13f, 2.15f, new Vector2(1562, 307), new Vector2(1480, 351), red);
        Laser(time + 1.1f, 2.7f, new Vector2(1362, 530), new Vector2(1434, 590), red);

        batch.End();
    }

    private void DrawClouds(float time)
    {
        var smoke = new Color(184, 144, 132);
        var nebula = new Color(114, 127, 201);
        Cloud(time, new Vector2(1070, 746), smoke, 4f);
        Cloud(time, new Vector2(1225, 833), smoke, 1f);
        Cloud(time, new Vector2(1430, 761), nebula, 3f);
    }

    private void Cloud(float time, Vector2 center, Color color, float offset, float width = 270, float height = 76)
    {
        var phase = (time / 15f + offset / 5f) % 1f;
        var opacity = 0.11f * MathF.Sin(MathF.PI * phase);
        var drift = new Vector2(102f * (phase - 0.5f), -25f * phase);
        Light(center + drift, width, height, color, opacity, -0.18f);
        Light(center + drift + new Vector2(41, -20), width * 0.57f, height * 0.66f, color, opacity * 0.7f, -0.35f);
    }

    private void DrawMaskedArtwork(float time, Matrix muralTransform)
    {
        batch.Begin(samplerState: SamplerState.LinearClamp, rasterizerState: muralRasterizer, transformMatrix: muralTransform);
        DrawDriftingMist(RuinsFog, time, 8.5f, 103f, 0.96f);
        DrawDriftingMist(MoonClouds, time, 9.5f, 108f, 1f);
        DrawGalaxy(time);
        DrawBanner(time);
        DrawDragonFire(time);
        DrawCape(VampireCape, time, 1.9f, 0.1f, 10f, VampireCapeMaskOrigin);
        DrawCape(HeroCape, time, 3.5f, 1.8f, 8f, HeroCapeMaskOrigin);
        DrawCape(KnightUpperCape, time, 2.4f, 2.4f, 8f);
        DrawCape(KnightLowerCape, time, 2.2f, 4.1f, 10f);
        DrawCape(WizardRobe, time, 2.0f, 3.5f, 7.5f);
        DrawCape(VillainCape, time, 2.6f, 1.1f, 11f);
        DrawCape(WerewolfFur, time, 3.4f, 2.1f, 6f, WerewolfFurMaskOrigin);

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

        batch.End();
    }

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
            var source = region;
            source.Offset(-MaskArea.X, -MaskArea.Y);
            batch.Draw(artworkMask, position, source, Color.White * opacity);
        }
    }

    private void DrawGalaxy(float time)
    {
        var source = Galaxy;
        source.Offset(-MaskArea.X, -MaskArea.Y);
        var origin = GalaxyCenter - new Vector2(Galaxy.X, Galaxy.Y);
        batch.Draw(artworkMask, GalaxyCenter, source,
            new Color(0.24f, 0.28f, 0.49f) * 0.94f, 0f, origin, 1f, SpriteEffects.None, 0f);

        foreach (var (atlasOrigin, _, _, angularSpeed) in GalaxyRings)
        {
            var ring = new Rectangle(atlasOrigin.X, atlasOrigin.Y, Galaxy.Width, Galaxy.Height);
            var rotation = (time * angularSpeed) % MathHelper.TwoPi;
            batch.Draw(artworkMask, GalaxyCenter, ring, Color.White, rotation, origin,
                1f, SpriteEffects.None, 0f);
        }
    }

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
            var source = new Rectangle(DragonFire.X - MaskArea.X, y - MaskArea.Y, DragonFire.Width, 1);
            batch.Draw(artworkMask, new Vector2(DragonFire.X + shift, y), source,
                new Color(brightness, brightness, brightness) * 0.83f);
        }
    }

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
            Light(position, 4 + i % 4, 2.4f, color, 0.64f * fade, 0.75f - scatter * 0.52f);
        }
    }

    private void DrawWizardMagic(float time)
    {
        var source = WizardMagic;
        source.Offset(-MaskArea.X, -MaskArea.Y);
        var origin = WizardStaffHead - new Vector2(WizardMagic.X, WizardMagic.Y);
        var pulse = 0.5f + 0.5f * MathF.Sin(time * 3.4f);
        var rotation = 0.21f * MathF.Sin(time * 2.1f) + 0.09f * MathF.Sin(time * 4.2f);
        var scale = 0.95f + 0.10f * pulse;

        var blue = new Color(121, 151, 255) { A = (byte)(32f + 48f * pulse) };
        batch.Draw(artworkMask, WizardStaffHead, source, blue, rotation, origin,
            scale, SpriteEffects.None, 0f);
        var violet = new Color(178, 158, 255) { A = (byte)(23f + 27f * (1f - pulse)) };
        batch.Draw(artworkMask, WizardStaffHead, source, violet, -rotation * 0.75f, origin,
            1.03f, SpriteEffects.None, 0f);
    }

    private void DrawCape(Rectangle region, float time, float rate, float offset, float amplitude,
        Point? maskOrigin = null)
    {
        var origin = maskOrigin ?? region.Location;
        var cycle = time * rate + offset;
        var rowTop = (float)region.Top;
        for (var y = region.Top; y < region.Bottom; y++)
        {
            var distance = (y - region.Top) / (float)region.Height;
            var phase = cycle - distance * 9f;
            var wave = 0.78f * MathF.Sin(phase) + 0.22f * MathF.Sin(phase * 1.7f + 0.9f);
            var shift = amplitude * wave * (0.45f + 0.55f * distance);
            var brightness = 0.85f + 0.15f * MathF.Sin(phase + 0.8f);

            var nextDistance = (y + 1 - region.Top) / (float)region.Height;
            var lift = amplitude * 0.38f * MathF.Sin(cycle - nextDistance * 7f)
                * MathF.Sin(nextDistance * MathF.PI);
            var rowBottom = y + 1 + lift;
            var source = new Rectangle(origin.X, origin.Y + y - region.Y, region.Width, 1);
            var x = region.X == 0 ? shift * 0.5f + amplitude * 0.5f : region.X + shift;
            batch.Draw(artworkMask, new Vector2(x, rowTop), source,
                new Color(brightness, brightness, brightness) * 0.87f, 0f, Vector2.Zero,
                new Vector2(1f, rowBottom - rowTop), SpriteEffects.None, 0f);
            rowTop = rowBottom;
        }
    }

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
            batch.Draw(mural, new Vector2(left.X, y), left, Color.White, 0f, Vector2.Zero,
                new Vector2(1f + shift / halfWidth, 1f), SpriteEffects.None, 0f);
            batch.Draw(mural, new Vector2(right.X + shift, y), right, Color.White, 0f, Vector2.Zero,
                new Vector2(1f - shift / halfWidth, 1f), SpriteEffects.None, 0f);
        }
    }

    private void DimPaintedPixels(Rectangle region, float opacity)
    {
        var source = region;
        source.Offset(-MaskArea.X, -MaskArea.Y);
        batch.Draw(artworkMask, region, source, Color.White * opacity);
    }

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

    private void DrawLightningSegment(Vector2 start, Vector2 end, float intensity)
    {
        var direction = end - start;
        var angle = MathF.Atan2(direction.Y, direction.X);
        var length = direction.Length();
        Light((start + end) * 0.5f, length + 12f, 11f, new Color(102, 255, 59), intensity * 0.44f, angle);
        var core = new Color(207, 255, 159) { A = (byte)(intensity * 255f) };
        var centerPixel = new Rectangle(glow.Width / 2, glow.Height / 2, 1, 1);
        batch.Draw(glow, start, centerPixel, core, angle, new Vector2(0f, 0.5f),
            new Vector2(length + 0.8f, 1.55f), SpriteEffects.None, 0f);
    }

    private static float EnginePulse(float time) =>
        0.21f + 0.07f * MathF.Sin(10 * time) + 0.03f * MathF.Sin(17 * time);

    private void Laser(float time, float interval, Vector2 start, Vector2 end, Color color)
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
        Light(center, length * 1.5f, 23f, color, 0.72f * envelope, angle);
        Light(center, length, 7f, new Color(255, 139, 127), 0.86f * envelope, angle);
        Light(head, 23f, 13f, new Color(255, 213, 184), 0.70f * envelope, angle);

        var core = new Color(255, 205, 183) { A = (byte)(0.84f * envelope * 255f) };
        var centerPixel = new Rectangle(glow.Width / 2, glow.Height / 2, 1, 1);
        batch.Draw(glow, head - direction * length, centerPixel, core, angle,
            new Vector2(0f, 0.5f), new Vector2(length, 2.3f), SpriteEffects.None, 0f);
    }

    private void Light(Vector2 center, float width, float height, Color color, float intensity, float angle = 0)
    {
        color.A = (byte)MathF.Round(MathHelper.Clamp(intensity * 1.32f, 0f, 1f) * 255);
        var origin = new Vector2(glow.Width / 2f, glow.Height / 2f);
        var scale = new Vector2(width / glow.Width, height / glow.Height);
        batch.Draw(glow, center, null, color, angle, origin, scale, SpriteEffects.None, 0);
    }
}
