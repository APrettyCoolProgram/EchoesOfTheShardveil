using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TheCards.Desktop;

/// <summary>Animates details in the ultrawide artwork using coordinates in the source image.</summary>
internal sealed class SideSceneEffects : IDisposable
{
    private const int SectorCount = 8;
    private static readonly Rectangle GillArea = new(754, 516, 270, 312);
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
    private readonly Texture2D leaf;
    private readonly Texture2D gillMasks;

    public SideSceneEffects(Texture2D artwork)
    {
        gillMasks = CreateGillMasks(artwork);
        leaf = CreateLeaf(artwork.GraphicsDevice);
    }

    /// <summary>Draws tumbling leaves blowing left to right across the forest path.</summary>
    public void DrawLeaves(SpriteBatch batch, Rectangle source, Matrix transform, float time)
    {
        const int leafCount = 17;
        var scale = new Vector2(transform.M11, transform.M22);
        for (var i = 0; i < leafCount; i++)
        {
            var clock = time * (0.09f + i % 5 * 0.013f) + i * 0.618f;
            var progress = clock - MathF.Floor(clock);
            var depth = (i * 7 % leafCount) / (float)(leafCount - 1);
            var startX = MathHelper.Lerp(248, 88, depth);
            var endX = MathHelper.Lerp(360, 380, depth);
            var bounce = MathF.Abs(MathF.Sin(progress * 12 + i)) * MathHelper.Lerp(2, 10, depth);
            var position = new Vector2(MathHelper.Lerp(startX, endX, progress),
                MathHelper.Lerp(912, 1310, depth) + 24 * progress - bounce);
            var margin = Math.Min(Math.Min(position.X - source.Left, source.Right - position.X),
                Math.Min(position.Y - source.Top, source.Bottom - position.Y));
            var opacity = Math.Clamp(margin / 24, 0, 1) *
                Math.Clamp(Math.Min(progress, 1 - progress) * 8, 0, 1);
            var tumble = time * (2.6f + i % 4 * 0.5f) + i * 2.4f;
            var size = (0.23f + 0.65f * depth) * (0.85f + i % 3 * 0.17f);
            var leafScale = scale * new Vector2(size, size * (0.2f + 0.8f * MathF.Abs(MathF.Sin(tumble))));
            batch.Draw(leaf, Vector2.Transform(position, transform), null, LeafColors[i % LeafColors.Length] * opacity,
                tumble, new Vector2(leaf.Width / 2f, leaf.Height / 2f), leafScale, SpriteEffects.None, 0);
        }
    }

    /// <summary>Sweeps color clockwise around the painted gills without moving the caps or stems.</summary>
    public void DrawGills(SpriteBatch batch, Rectangle crop, Matrix transform, float time)
    {
        var visible = Rectangle.Intersect(crop, GillArea);
        if (visible.Width == 0 || visible.Height == 0)
        {
            return;
        }

        var position = Vector2.Transform(visible.Location.ToVector2(), transform);
        var scale = new Vector2(transform.M11, transform.M22);
        for (var sector = 0; sector < SectorCount; sector++)
        {
            var phase = (sector / (float)SectorCount - time / 12.8f) * MathHelper.TwoPi;
            var color = new Color(
                0.54f + 0.46f * MathF.Cos(phase),
                0.54f + 0.46f * MathF.Cos(phase - MathHelper.TwoPi / 3),
                0.54f + 0.46f * MathF.Cos(phase + MathHelper.TwoPi / 3));
            var mask = new Rectangle(visible.X - GillArea.X, visible.Y - GillArea.Y + sector * GillArea.Height,
                visible.Width, visible.Height);
            batch.Draw(gillMasks, position, mask, color * 0.64f, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
        }
    }

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

        var texture = new Texture2D(artwork.GraphicsDevice, GillArea.Width, GillArea.Height * SectorCount);
        texture.SetData(masks);
        return texture;
    }

    private static float OutlineCoverage(Vector2 point, Vector2[] outline)
    {
        var inside = false;
        var distanceSquared = float.PositiveInfinity;
        for (var i = 0; i < outline.Length; i++)
        {
            var a = outline[i];
            var b = outline[(i + 1) % outline.Length];
            var edge = b - a;
            if ((a.Y > point.Y) != (b.Y > point.Y) && point.X < a.X + (point.Y - a.Y) * edge.X / edge.Y)
            {
                inside = !inside;
            }
            var projection = Math.Clamp(Vector2.Dot(point - a, edge) / edge.LengthSquared(), 0, 1);
            distanceSquared = Math.Min(distanceSquared, Vector2.DistanceSquared(point, a + edge * projection));
        }

        return inside ? Math.Clamp(MathF.Sqrt(distanceSquared) / 3, 0, 1) : 0;
    }

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

        var texture = new Texture2D(device, width, height);
        texture.SetData(pixels);
        return texture;
    }

    public void Dispose()
    {
        leaf.Dispose();
        gillMasks.Dispose();
    }
}
