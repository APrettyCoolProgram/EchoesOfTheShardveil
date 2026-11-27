// 261127_code
// 261127_documentation

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EchoesOfTheShardveil.Desktop;

/// <summary>Positions drawing surfaces in the window and maps mouse positions back to them.</summary>
internal static class ScreenLayout
{
    /// <summary>Gets the largest centered rectangle with the surface's shape that fits in the viewport.</summary>
    /// <param name="viewport">The window viewport.</param>
    /// <param name="width">The surface width.</param>
    /// <param name="height">The surface height.</param>
    /// <returns>The fitted rectangle in viewport pixels.</returns>
    public static Rectangle Fit(Viewport viewport, int width, int height)
    {
        var scale = Math.Min((float)viewport.Width / width, (float)viewport.Height / height);
        var fittedWidth = (int)MathF.Round(width * scale);
        var fittedHeight = (int)MathF.Round(height * scale);
        return new((viewport.Width - fittedWidth) / 2, (viewport.Height - fittedHeight) / 2,
            fittedWidth, fittedHeight);
    }

    /// <summary>Gets the smallest centered rectangle with the surface's shape that covers the viewport.</summary>
    /// <param name="viewport">The window viewport.</param>
    /// <param name="width">The surface width.</param>
    /// <param name="height">The surface height.</param>
    /// <returns>The covering rectangle in viewport pixels, which may extend past the edges.</returns>
    public static Rectangle Cover(Viewport viewport, int width, int height)
    {
        var scale = Math.Max((double)viewport.Width / width, (double)viewport.Height / height);
        var coveredWidth = (int)Math.Ceiling(width * scale);
        var coveredHeight = (int)Math.Ceiling(height * scale);
        return new((viewport.Width - coveredWidth) / 2, (viewport.Height - coveredHeight) / 2,
            coveredWidth, coveredHeight);
    }

    /// <summary>Places an overlay along the bottom of the viewport, scaled with a fitted surface.</summary>
    /// <param name="viewport">The window viewport.</param>
    /// <param name="fitted">The rectangle the surface was fitted to.</param>
    /// <param name="surfaceHeight">The unscaled height of the surface.</param>
    /// <param name="overlayHeight">The unscaled height of the overlay.</param>
    /// <returns>The overlay rectangle in viewport pixels.</returns>
    public static Rectangle BottomOverlay(Viewport viewport, Rectangle fitted, int surfaceHeight, int overlayHeight)
    {
        var height = (int)MathF.Round(overlayHeight * ((float)fitted.Height / surfaceHeight));
        return new(fitted.X, viewport.Height - height, fitted.Width, height);
    }

    /// <summary>Converts a mouse position from client units to viewport pixels.</summary>
    /// <param name="mouse">The mouse position in client units.</param>
    /// <param name="client">The window's client size, which must not be empty.</param>
    /// <param name="viewport">The window viewport.</param>
    /// <returns>The position in viewport pixels.</returns>
    public static Vector2 ToViewportPixels(Point mouse, Rectangle client, Viewport viewport) =>
        new(mouse.X * ((float)viewport.Width / client.Width), mouse.Y * ((float)viewport.Height / client.Height));

    /// <summary>Converts a mouse position to the coordinates of a surface drawn in the viewport.</summary>
    /// <param name="mouse">The mouse position in client units.</param>
    /// <param name="client">The window's client size in mouse units.</param>
    /// <param name="viewport">The window viewport.</param>
    /// <param name="fitted">Where the surface is drawn, in viewport pixels.</param>
    /// <param name="width">The unscaled surface width.</param>
    /// <param name="height">The unscaled surface height.</param>
    /// <param name="position">The surface position, if the mouse is over the surface.</param>
    /// <returns><see langword="true"/> if the mouse is over the surface.</returns>
    public static bool TryMap(Point mouse, Rectangle client, Viewport viewport, Rectangle fitted,
        int width, int height, out Point position)
    {
        position = default;
        if (client.Width <= 0 || client.Height <= 0 || fitted.Width <= 0 || fitted.Height <= 0)
        {
            return false;
        }

        var pixel = ToViewportPixels(mouse, client, viewport);
        var x = pixel.X - fitted.X;
        var y = pixel.Y - fitted.Y;
        if (x < 0 || y < 0 || x >= fitted.Width || y >= fitted.Height)
        {
            return false;
        }

        position = new((int)(x * width / fitted.Width), (int)(y * height / fitted.Height));
        return true;
    }
}
