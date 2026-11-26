using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TheCards.Desktop;

/// <summary>Draws the startup screen within the game window.</summary>
internal sealed class MainUI(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, Texture2D mural,
    Texture2D title, Texture2D menu, MuralEffects effects, TitleReveal reveal, RasterizerState muralRasterizer)
{
    private const int MenuItemSpacing = 258;

    /// <summary>Tests the Single player label in the bottom-anchored menu.</summary>
    public bool IsSinglePlayer(Point mouse, Rectangle clientBounds)
    {
        var viewport = graphicsDevice.Viewport;
        var fitted = ScreenLayout.Fit(viewport, mural.Width, mural.Height);
        var menuBounds = ScreenLayout.BottomOverlay(viewport, fitted, mural.Height, menu.Height);
        return ScreenLayout.TryMap(mouse, clientBounds, viewport, menuBounds,
            menu.Width, menu.Height, out var position)
            && new Rectangle((menu.Width + MenuItemSpacing) / 2 - 120, 24, 240, 78)
                .Contains(position);
    }

    /// <summary>Fills the viewport with the mural and anchors the logo and menu to the window edges.</summary>
    public void Draw(GameTime gameTime)
    {
        var viewport = graphicsDevice.Viewport;
        if (viewport.Width <= 0 || viewport.Height <= 0)
        {
            return;
        }

        var destination = ScreenLayout.Cover(viewport, mural.Width, mural.Height);
        var fitted = ScreenLayout.Fit(viewport, mural.Width, mural.Height);
        spriteBatch.Begin(samplerState: SamplerState.LinearClamp);
        spriteBatch.Draw(mural, destination, Color.White);
        spriteBatch.End();

        var muralTransform = Matrix.CreateScale(
            (float)destination.Width / mural.Width,
            (float)destination.Height / mural.Height, 1f)
            * Matrix.CreateTranslation(destination.X, destination.Y, 0f);
        const int titleWidth = 416;
        var titleHeight = (int)MathF.Round((float)title.Height / title.Width * titleWidth);
        var titleBounds = new Rectangle((mural.Width - titleWidth) / 2, 10, titleWidth, titleHeight);
        var titleTransform = Matrix.CreateScale(
            (float)fitted.Width / mural.Width, (float)fitted.Height / mural.Height, 1f)
            * Matrix.CreateTranslation(fitted.X, 0f, 0f);
        var time = (float)gameTime.TotalGameTime.TotalSeconds;
        var previousScissor = graphicsDevice.ScissorRectangle;
        var previousRasterizer = graphicsDevice.RasterizerState;
        graphicsDevice.ScissorRectangle = viewport.Bounds;
        try
        {
            effects.Draw(time, muralTransform);
            reveal.Draw(spriteBatch, titleBounds, titleTransform, muralRasterizer, time);
        }
        finally
        {
            graphicsDevice.RasterizerState = previousRasterizer;
            graphicsDevice.ScissorRectangle = previousScissor;
        }

        DrawMenu(viewport, ScreenLayout.BottomOverlay(viewport, fitted, mural.Height, menu.Height));
    }

    private void DrawMenu(Viewport viewport, Rectangle bounds)
    {
        spriteBatch.Begin(samplerState: SamplerState.LinearClamp);
        spriteBatch.Draw(menu, bounds, Color.White);
        if (bounds.Left > 0)
        {
            spriteBatch.Draw(menu, new Rectangle(0, bounds.Y, bounds.Left, bounds.Height),
                new Rectangle(0, 0, 1, menu.Height), Color.White);
        }
        if (bounds.Right < viewport.Width)
        {
            spriteBatch.Draw(menu, new Rectangle(bounds.Right, bounds.Y, viewport.Width - bounds.Right, bounds.Height),
                new Rectangle(menu.Width - 1, 0, 1, menu.Height), Color.White);
        }
        spriteBatch.End();
    }
}
