using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TheCards.Desktop;

internal enum WindowControlResult { Ignored, Handled, ConfirmedExit }

/// <summary>Draws the upper-right controls and handles their modal exit confirmation.</summary>
internal sealed class WindowControls(GameAudio audio, bool isBoard)
{
    private const int OriginalIconSize = 44;
    private const int IconSize = OriginalIconSize * 3 / 4;
    private const int IconMargin = 20;
    private const int IconGap = 10;
    private const float IconScale = (float)IconSize / OriginalIconSize;

    private static readonly Color Ivory = new(240, 234, 219);
    private static readonly Color Border = new(82, 113, 120);
    private static readonly Color Panel = new(23, 38, 46);
    private static readonly Vector2[] Ring = Enumerable.Range(0, 41)
        .Select(i => new Vector2(
            22f + 20f * MathF.Cos(i * MathHelper.TwoPi / 40),
            22f + 20f * MathF.Sin(i * MathHelper.TwoPi / 40)))
        .ToArray();
    private static readonly Vector2[][] ExitMark =
    [
        [new(25, 12), new(12, 12), new(12, 32), new(25, 32)],
        [new(19, 22), new(33, 22)],
        [new(28, 17), new(33, 22), new(28, 27)],
    ];
    private static readonly Vector2[][] SpeakerMark =
    [
        [new(11, 18), new(16, 18), new(23, 12), new(23, 32), new(16, 26), new(11, 26), new(11, 18)],
    ];
    private static readonly Vector2[][] SoundWaves =
    [
        [new(28, 17), new(30, 22), new(28, 27)],
        [new(32, 14), new(35, 19), new(35, 25), new(32, 30)],
    ];
    private static readonly Vector2[][] MuteMark =
    [
        [new(28, 18), new(35, 25)],
        [new(35, 18), new(28, 25)],
    ];

    public bool IsConfirming { get; private set; }

    public void ToggleExitPrompt() => IsConfirming = !IsConfirming;

    public void RequestExitConfirmation() => IsConfirming = true;

    public WindowControlResult Click(Point mouse, Rectangle client, Viewport viewport)
    {
        if (client.Width <= 0 || client.Height <= 0 || viewport.Width <= 0 || viewport.Height <= 0)
        {
            return IsConfirming ? WindowControlResult.Handled : WindowControlResult.Ignored;
        }

        var layout = new Layout(viewport);
        var pointer = new Point(
            (int)(mouse.X * ((float)viewport.Width / client.Width) / layout.Scale),
            (int)(mouse.Y * ((float)viewport.Height / client.Height) / layout.Scale));
        if (IsConfirming)
        {
            if (layout.Confirm.Contains(pointer))
            {
                IsConfirming = false;
                return WindowControlResult.ConfirmedExit;
            }
            if (layout.Cancel.Contains(pointer))
            {
                IsConfirming = false;
            }
            return WindowControlResult.Handled;
        }

        if (ContainsIcon(layout.Exit, pointer))
        {
            RequestExitConfirmation();
            return WindowControlResult.Handled;
        }
        if (ContainsIcon(layout.Sound, pointer))
        {
            audio.Toggle();
            return WindowControlResult.Handled;
        }
        return WindowControlResult.Ignored;
    }

    public void Draw(SpriteBatch batch, Texture2D pixel, Texture2D font, Viewport viewport)
    {
        if (viewport.Width <= 0 || viewport.Height <= 0)
        {
            return;
        }

        var layout = new Layout(viewport);
        batch.Begin(samplerState: SamplerState.LinearClamp,
            transformMatrix: Matrix.CreateScale(layout.Scale, layout.Scale, 1));
        DrawIcon(batch, pixel, layout.Exit, 1f / layout.Scale, ExitMark);
        DrawIcon(batch, pixel, layout.Sound, 1f / layout.Scale, SpeakerMark, audio.Enabled ? SoundWaves : MuteMark);
        if (IsConfirming)
        {
            batch.Draw(pixel, new Rectangle(0, 0, layout.Width, layout.Height), Color.Black * 0.72f);
            Box(batch, pixel, layout.Dialog, Panel);
            var left = layout.Dialog.Left + 30;
            BitmapText.Draw(batch, font, isBoard ? "Forfeit the game?" : "Exit The Cards?",
                left, layout.Dialog.Top + 23, 32, Ivory);
            BitmapText.Draw(batch, font, isBoard ? "You will return to the main menu." : "The application will close.",
                left, layout.Dialog.Top + 69, 19, new Color(177, 192, 196));
            Button(batch, pixel, font, layout.Cancel, "Cancel", Panel);
            Button(batch, pixel, font, layout.Confirm, isBoard ? "Forfeit & return" : "Exit", new Color(117, 57, 61));
        }
        batch.End();
    }

    private static bool ContainsIcon(Rectangle bounds, Point pointer)
    {
        var radius = bounds.Width / 2f;
        var delta = new Vector2(pointer.X - bounds.X - radius, pointer.Y - bounds.Y - radius);
        return delta.LengthSquared() <= radius * radius;
    }

    private static void DrawIcon(SpriteBatch batch, Texture2D pixel, Rectangle bounds, float borderWidth,
        Vector2[][] mark, Vector2[][]? extra = null)
    {
        FillIconBackground(batch, pixel, bounds);
        var origin = new Vector2(bounds.X, bounds.Y);
        DrawPath(batch, pixel, Ring, origin, borderWidth);
        foreach (var path in mark)
        {
            DrawPath(batch, pixel, path, origin, 1.8f * IconScale);
        }
        if (extra is not null)
        {
            foreach (var path in extra)
            {
                DrawPath(batch, pixel, path, origin, 1.8f * IconScale);
            }
        }
    }

    private static void DrawPath(SpriteBatch batch, Texture2D pixel, Vector2[] path, Vector2 origin, float width)
    {
        for (var i = 1; i < path.Length; i++)
        {
            var delta = (path[i] - path[i - 1]) * IconScale;
            batch.Draw(pixel, origin + path[i - 1] * IconScale, null, Ivory, MathF.Atan2(delta.Y, delta.X),
                new Vector2(0f, 0.5f), new Vector2(delta.Length() + width * 0.40f, width), SpriteEffects.None, 0f);
        }
    }

    private static void FillIconBackground(SpriteBatch batch, Texture2D pixel, Rectangle bounds)
    {
        var center = bounds.Width / 2f;
        var radius = 20f * IconScale;
        for (var y = 0; y < bounds.Height; y++)
        {
            var distance = y + 0.5f - center;
            if (MathF.Abs(distance) >= radius)
            {
                continue;
            }

            var halfSpan = MathF.Sqrt(radius * radius - distance * distance);
            var left = (int)MathF.Round(center - halfSpan);
            var right = (int)MathF.Round(center + halfSpan);
            batch.Draw(pixel, new Rectangle(bounds.X + left, bounds.Y + y, right - left, 1), Color.Black);
        }
    }

    private static void Box(SpriteBatch batch, Texture2D pixel, Rectangle bounds, Color fill)
    {
        batch.Draw(pixel, bounds, Border);
        bounds.Inflate(-1, -1);
        batch.Draw(pixel, bounds, fill);
    }

    private static void Button(SpriteBatch batch, Texture2D pixel, Texture2D font,
        Rectangle bounds, string label, Color fill)
    {
        Box(batch, pixel, bounds, fill);
        const int height = 20;
        BitmapText.Draw(batch, font, label, bounds.Center.X - BitmapText.Advance(height) * label.Length / 2,
            bounds.Center.Y - height / 2, height, Ivory);
    }

    private readonly record struct Layout
    {
        public Layout(Viewport viewport)
        {
            Scale = Math.Min(viewport.Width / 1280f, viewport.Height / 720f);
            Width = (int)MathF.Ceiling(viewport.Width / Scale);
            Height = (int)MathF.Ceiling(viewport.Height / Scale);
        }

        public float Scale { get; }
        public int Width { get; }
        public int Height { get; }
        public Rectangle Exit => new(Width - IconMargin - IconSize, IconMargin, IconSize, IconSize);
        public Rectangle Sound => new(Exit.X, Exit.Bottom + IconGap, IconSize, IconSize);
        public Rectangle Dialog => new((Width - 500) / 2, (Height - 172) / 2, 500, 172);
        public Rectangle Cancel => new(Dialog.Right - 324, Dialog.Bottom - 58, 108, 37);
        public Rectangle Confirm => new(Dialog.Right - 203, Dialog.Bottom - 58, 176, 37);
    }
}
