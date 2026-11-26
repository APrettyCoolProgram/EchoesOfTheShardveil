using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TheCards.Desktop;

/// <summary>What the host game should do after a screen's update.</summary>
internal enum ScreenTransition { None, OpenBoard, ReturnToMenu, ExitGame }

/// <summary>Graphics and audio owned by the host game and shared by every screen.</summary>
internal sealed record GameServices(GraphicsDevice Device, SpriteBatch Batch, Texture2D Pixel, Texture2D Font,
    GameAudio Audio);

/// <summary>One full-window state of the game, such as the main menu or the board.</summary>
internal interface IScreen : IDisposable
{
    string Title { get; }

    /// <summary>Handles input for one frame. <paramref name="client"/> is the window's client size in mouse units.</summary>
    ScreenTransition Update(GameTime gameTime, Rectangle client, bool isActive);

    void Draw(GameTime gameTime);

    /// <summary>Responds to the window's close button, normally by showing the exit prompt.</summary>
    void RequestClose();
}
