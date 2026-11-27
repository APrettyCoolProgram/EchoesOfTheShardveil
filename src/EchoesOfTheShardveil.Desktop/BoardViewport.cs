// 261127_code
// 261127_documentation

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EchoesOfTheShardveil.Desktop;

/// <summary>Maps a uniformly scaled board to a full window, allowing the layout to grow on either axis.</summary>
internal readonly record struct BoardViewport
{
    private readonly Viewport viewport;
    private readonly float scale;

    /// <summary>Initializes the mapping for a window viewport.</summary>
    /// <param name="viewport">The window viewport in pixels.</param>
    public BoardViewport(Viewport viewport)
    {
        this.viewport = viewport;
        scale = Math.Min((float)viewport.Width / BoardUI.Width, (float)viewport.Height / BoardUI.Height);
    }

    /// <summary>Gets whether the window has any area to draw in.</summary>
    public bool IsDrawable => scale > 0;

    /// <summary>Gets the board layout height in design units, at least <see cref="BoardUI.Height"/>.</summary>
    public int Height => IsDrawable ? (int)MathF.Ceiling(viewport.Height / scale) : BoardUI.Height;

    /// <summary>Gets the board layout width in design units, at least <see cref="BoardUI.Width"/>.</summary>
    public int Width => IsDrawable ? (int)MathF.Ceiling(viewport.Width / scale) : BoardUI.Width;

    /// <summary>Gets the transform from design units to window pixels.</summary>
    public Matrix Transform => Matrix.CreateScale(scale, scale, 1f);

    /// <summary>Converts a mouse position to board design units.</summary>
    /// <param name="mouse">The mouse position in client units.</param>
    /// <param name="client">The window's client size in mouse units.</param>
    /// <param name="position">The board position, if the mouse is inside the window.</param>
    /// <returns><see langword="true"/> if the mouse is over the board.</returns>
    public bool TryMap(Point mouse, Rectangle client, out Point position)
    {
        position = default;
        if (!IsDrawable || client.Width <= 0 || client.Height <= 0 ||
            mouse.X < 0 || mouse.X >= client.Width || mouse.Y < 0 || mouse.Y >= client.Height)
        {
            return false;
        }

        var pixel = ScreenLayout.ToViewportPixels(mouse, client, viewport);
        position = new((int)(pixel.X / scale), (int)(pixel.Y / scale));
        return true;
    }
}
