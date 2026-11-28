// 261127_code
// 261127_documentation

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EchoesOfTheShardveil.Desktop;

/// <summary>Draws the startup screen within the game window.</summary>
/// <param name="graphicsDevice">The graphics device.</param>
/// <param name="spriteBatch">The sprite batch shared with the effects.</param>
/// <param name="mural">The main painting.</param>
/// <param name="titleTexture">The wordmark artwork.</param>
/// <param name="menuBar">The menu artwork along the bottom.</param>
/// <param name="muralEffects">The animations on the mural.</param>
/// <param name="titleReveal">The title animation.</param>
/// <param name="muralRasterizer">A rasterizer state with scissoring enabled.</param>
internal sealed class MainUI(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, Texture2D mural,
    Texture2D titleTexture, Texture2D menuBar, FourWorldMuralEffects muralEffects,
    TitleReveal titleReveal, RasterizerState muralRasterizer)
{
    private const int MenuItemSpacing = 258;
    private const float TitleScale = 0.7425f;

    /// <summary>Finds the screen selected in the bottom-anchored menu.</summary>
    /// <param name="mouse">The mouse position in client units.</param>
    /// <param name="clientBounds">The window's client size in mouse units.</param>
    /// <returns>The requested screen, or no transition if the pointer is outside the supported labels.</returns>
    public ScreenTransition MenuSelection(Point mouse, Rectangle clientBounds)
    {
        var viewport = graphicsDevice.Viewport;
        var fitted = ScreenLayout.Fit(viewport, mural.Width, mural.Height);
        var menuBounds = ScreenLayout.BottomOverlay(viewport, fitted, mural.Height, menuBar.Height);
        // The label's position is in the menu artwork's own pixels.
        if (!ScreenLayout.TryMap(mouse, clientBounds, viewport, menuBounds,
            menuBar.Width, menuBar.Height, out var position))
        {
            return ScreenTransition.None;
        }

        var deckbuilder = new Rectangle((menuBar.Width - MenuItemSpacing) / 2 - 120, 24, 240, 78);
        var singlePlayer = new Rectangle((menuBar.Width + MenuItemSpacing) / 2 - 120, 24, 240, 78);
        if (deckbuilder.Contains(position))
        {
            return ScreenTransition.OpenDeckbuilder;
        }
        return singlePlayer.Contains(position) ? ScreenTransition.OpenBoard : ScreenTransition.None;
    }

    /// <summary>Fills the viewport with the mural and anchors the logo and menu to the window edges.</summary>
    /// <param name="gameTime">Timing measured from when the menu was shown.</param>
    public void Draw(GameTime gameTime)
    {
        var viewport = graphicsDevice.Viewport;
        if (viewport.Width <= 0 || viewport.Height <= 0)
        {
            return;
        }

        var fitted = ScreenLayout.Fit(viewport, mural.Width, mural.Height);
        // Keep the outer duels in frame when a window is narrower than the four-world painting.
        var destination = fitted.Height < viewport.Height
            ? fitted
            : ScreenLayout.Cover(viewport, mural.Width, mural.Height);
        var time = (float)gameTime.TotalGameTime.TotalSeconds;
        spriteBatch.Begin(samplerState: SamplerState.LinearClamp);
        spriteBatch.Draw(mural, destination, Color.White);
        spriteBatch.End();

        var muralTransform = Matrix.CreateScale(
            (float)destination.Width / mural.Width,
            (float)destination.Height / mural.Height, 1f)
            * Matrix.CreateTranslation(destination.X, destination.Y, 0f);
        // The wordmark is drawn at a fixed scale (about 815 of the mural's 1,672 units wide), so its letters keep their size.
        var titleWidth = (int)MathF.Round(titleTexture.Width * TitleScale);
        var titleHeight = (int)MathF.Round(titleTexture.Height * TitleScale);
        var titleBounds = new Rectangle((mural.Width - titleWidth) / 2, 10, titleWidth, titleHeight);
        // Unlike the mural, the title stays at the top of the window when the mural is cropped.
        var titleTransform = Matrix.CreateScale(
            (float)fitted.Width / mural.Width, (float)fitted.Height / mural.Height, 1f)
            * Matrix.CreateTranslation(fitted.X, 0f, 0f);
        var previousScissor = graphicsDevice.ScissorRectangle;
        var previousRasterizer = graphicsDevice.RasterizerState;
        graphicsDevice.ScissorRectangle = viewport.Bounds;
        try
        {
            muralEffects.Draw(time, muralTransform);
            titleReveal.Draw(spriteBatch, titleBounds, titleTransform, muralRasterizer, time);
        }
        finally
        {
            graphicsDevice.RasterizerState = previousRasterizer;
            graphicsDevice.ScissorRectangle = previousScissor;
        }

        DrawMenu(viewport, ScreenLayout.BottomOverlay(viewport, fitted, mural.Height, menuBar.Height));
    }

    /// <summary>Draws the menu bar, stretching its edge columns to the sides of a wider window.</summary>
    /// <param name="viewport">The window viewport.</param>
    /// <param name="bounds">The menu bar's area.</param>
    private void DrawMenu(Viewport viewport, Rectangle bounds)
    {
        spriteBatch.Begin(samplerState: SamplerState.LinearClamp);
        spriteBatch.Draw(menuBar, bounds, Color.White);
        if (bounds.Left > 0)
        {
            spriteBatch.Draw(menuBar, new Rectangle(0, bounds.Y, bounds.Left, bounds.Height),
                new Rectangle(0, 0, 1, menuBar.Height), Color.White);
        }
        if (bounds.Right < viewport.Width)
        {
            spriteBatch.Draw(menuBar, new Rectangle(bounds.Right, bounds.Y, viewport.Width - bounds.Right, bounds.Height),
                new Rectangle(menuBar.Width - 1, 0, 1, menuBar.Height), Color.White);
        }
        spriteBatch.End();
    }
}
