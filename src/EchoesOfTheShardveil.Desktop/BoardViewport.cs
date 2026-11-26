using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EchoesOfTheShardveil.Desktop;

/// <summary>Maps a uniformly scaled board to a full window, allowing the layout to grow on either axis.</summary>
internal readonly record struct BoardViewport
{
    private readonly Viewport viewport;
    private readonly float scale;

    public BoardViewport(Viewport viewport)
    {
        this.viewport = viewport;
        scale = Math.Min((float)viewport.Width / BoardUI.Width, (float)viewport.Height / BoardUI.Height);
    }

    public bool IsDrawable => scale > 0;
    public int Height => IsDrawable ? (int)MathF.Ceiling(viewport.Height / scale) : BoardUI.Height;
    public int Width => IsDrawable ? (int)MathF.Ceiling(viewport.Width / scale) : BoardUI.Width;
    public Matrix Transform => Matrix.CreateScale(scale, scale, 1f);

    public bool TryMap(Point mouse, Rectangle client, out Point position)
    {
        position = default;
        if (!IsDrawable || client.Width <= 0 || client.Height <= 0 ||
            mouse.X < 0 || mouse.X >= client.Width || mouse.Y < 0 || mouse.Y >= client.Height)
        {
            return false;
        }

        var x = mouse.X * ((float)viewport.Width / client.Width) / scale;
        var y = mouse.Y * ((float)viewport.Height / client.Height) / scale;
        position = new((int)x, (int)y);
        return true;
    }
}
