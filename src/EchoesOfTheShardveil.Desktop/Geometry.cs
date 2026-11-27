// 261127_code
// 261127_documentation

using Microsoft.Xna.Framework;

namespace EchoesOfTheShardveil.Desktop;

/// <summary>Shape tests shared by hit testing and the artwork masks.</summary>
internal static class Geometry
{
    /// <summary>Determines whether a point is on the circle inscribed in a square.</summary>
    /// <param name="bounds">The square around the circle.</param>
    /// <param name="point">The point to test.</param>
    /// <returns><see langword="true"/> if the point is inside the circle or on its rim.</returns>
    public static bool IsInCircle(Rectangle bounds, Point point)
    {
        var delta = point - bounds.Center;
        return delta.X * delta.X + delta.Y * delta.Y <= bounds.Width * bounds.Width / 4;
    }

    /// <summary>Determines whether a point is inside a polygon, using the even-odd rule.</summary>
    /// <param name="point">The point to test.</param>
    /// <param name="outline">The polygon's corners in order. The last one connects to the first.</param>
    /// <returns><see langword="true"/> if the point is inside the polygon.</returns>
    public static bool IsInPolygon(Vector2 point, ReadOnlySpan<Vector2> outline)
    {
        var inside = false;
        for (var i = 0; i < outline.Length; i++)
        {
            var start = outline[i];
            var end = outline[(i + 1) % outline.Length];
            if ((start.Y > point.Y) != (end.Y > point.Y)
                && point.X < start.X + (point.Y - start.Y) * (end.X - start.X) / (end.Y - start.Y))
            {
                inside = !inside;
            }
        }
        return inside;
    }

    /// <summary>Measures the squared distance from a point to a line segment.</summary>
    /// <param name="point">The point to measure from.</param>
    /// <param name="start">One end of the segment.</param>
    /// <param name="end">The other end, which must differ from <paramref name="start"/>.</param>
    /// <returns>The squared distance to the closest point of the segment.</returns>
    public static float DistanceSquaredToSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        var edge = end - start;
        var along = Math.Clamp(Vector2.Dot(point - start, edge) / edge.LengthSquared(), 0, 1);
        return Vector2.DistanceSquared(point, start + edge * along);
    }
}
