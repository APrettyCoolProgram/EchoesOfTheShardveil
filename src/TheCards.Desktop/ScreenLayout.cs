using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TheCards.Desktop;

/// <summary>Positions drawing surfaces in the window and maps mouse positions back to them.</summary>
internal static class ScreenLayout
{
    public static Rectangle Fit(Viewport viewport, int width, int height)
    {
        var scale = Math.Min((float)viewport.Width / width, (float)viewport.Height / height);
        var fittedWidth = (int)MathF.Round(width * scale);
        var fittedHeight = (int)MathF.Round(height * scale);
        return new((viewport.Width - fittedWidth) / 2, (viewport.Height - fittedHeight) / 2,
            fittedWidth, fittedHeight);
    }

    public static Rectangle Cover(Viewport viewport, int width, int height)
    {
        var scale = Math.Max((double)viewport.Width / width, (double)viewport.Height / height);
        var coveredWidth = (int)Math.Ceiling(width * scale);
        var coveredHeight = (int)Math.Ceiling(height * scale);
        return new((viewport.Width - coveredWidth) / 2, (viewport.Height - coveredHeight) / 2,
            coveredWidth, coveredHeight);
    }

    public static Rectangle BottomOverlay(Viewport viewport, Rectangle fitted, int surfaceHeight, int overlayHeight)
    {
        var height = (int)MathF.Round(overlayHeight * ((float)fitted.Height / surfaceHeight));
        return new(fitted.X, viewport.Height - height, fitted.Width, height);
    }

    public static bool TryMap(Point mouse, Rectangle client, Viewport viewport, Rectangle fitted,
        int width, int height, out Point position)
    {
        position = default;
        if (client.Width <= 0 || client.Height <= 0 || fitted.Width <= 0 || fitted.Height <= 0)
        {
            return false;
        }

        var x = mouse.X * ((float)viewport.Width / client.Width) - fitted.X;
        var y = mouse.Y * ((float)viewport.Height / client.Height) - fitted.Y;
        if (x < 0 || y < 0 || x >= fitted.Width || y >= fitted.Height)
        {
            return false;
        }

        position = new((int)(x * width / fitted.Width), (int)(y * height / fitted.Height));
        return true;
    }
}
