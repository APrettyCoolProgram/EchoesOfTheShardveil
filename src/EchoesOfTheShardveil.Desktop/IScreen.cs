using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EchoesOfTheShardveil.Desktop;

/// <summary>What the host game should do after a screen's update.</summary>
internal enum ScreenTransition
{
    /// <summary>Stay on the current screen.</summary>
    None,
    /// <summary>Start a duel.</summary>
    OpenBoard,
    /// <summary>Close the duel and show the menu.</summary>
    ReturnToMenu,
    /// <summary>Close the application.</summary>
    ExitGame,
}

/// <summary>Graphics and audio owned by the host game and shared by every screen.</summary>
/// <param name="Device">The graphics device.</param>
/// <param name="SpriteBatch">The sprite batch.</param>
/// <param name="Pixel">A one-pixel white texture for filling shapes.</param>
/// <param name="Font">The bitmap font sheet.</param>
/// <param name="Audio">The music player and sound preference.</param>
internal sealed record GameServices(GraphicsDevice Device, SpriteBatch SpriteBatch, Texture2D Pixel, Texture2D Font,
    GameAudio Audio);

/// <summary>One full-window state of the game, such as the main menu or the board.</summary>
internal interface IScreen : IDisposable
{
    /// <summary>Gets the window title to show while the screen is active.</summary>
    string Title { get; }

    /// <summary>Handles input and advances the screen by one frame.</summary>
    /// <param name="gameTime">The frame timing.</param>
    /// <param name="client">The window's client size in mouse units.</param>
    /// <param name="isActive">Whether the window has focus.</param>
    /// <returns>What the host should do next.</returns>
    ScreenTransition Update(GameTime gameTime, Rectangle client, bool isActive);

    /// <summary>Draws the screen.</summary>
    /// <param name="gameTime">The frame timing.</param>
    void Draw(GameTime gameTime);

    /// <summary>Responds to the window's close button, normally by showing the exit prompt.</summary>
    void RequestClose();
}
