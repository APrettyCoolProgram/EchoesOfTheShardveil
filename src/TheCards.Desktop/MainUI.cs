using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TheCards.Desktop;

/// <summary>Draws the startup screen within the game window.</summary>
internal sealed class MainUI(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, Texture2D mural,
    Texture2D title, Texture2D menu, MuralEffects effects)
{
    /// <summary>Centers the complete mural in the current viewport.</summary>
    public void Draw(GameTime gameTime)
    {
        var viewport = graphicsDevice.Viewport;
        var scale = Math.Min(
            (float)viewport.Width / mural.Width,
            (float)viewport.Height / mural.Height);
        var width = (int)MathF.Round(mural.Width * scale);
        var height = (int)MathF.Round(mural.Height * scale);
        if (width <= 0 || height <= 0)
        {
            return;
        }

        var destination = new Rectangle(
            (viewport.Width - width) / 2,
            (viewport.Height - height) / 2,
            width,
            height);

        spriteBatch.Begin(samplerState: SamplerState.LinearClamp);
        spriteBatch.Draw(mural, destination, Color.White);
        spriteBatch.End();

        var muralTransform = Matrix.CreateScale(
            (float)destination.Width / mural.Width,
            (float)destination.Height / mural.Height, 1f)
            * Matrix.CreateTranslation(destination.X, destination.Y, 0f);
        var previousScissor = graphicsDevice.ScissorRectangle;
        var previousRasterizer = graphicsDevice.RasterizerState;
        var muralScissor = destination;
        muralScissor.Offset(viewport.X, viewport.Y);
        graphicsDevice.ScissorRectangle = muralScissor;
        try
        {
            effects.Draw((float)gameTime.TotalGameTime.TotalSeconds, muralTransform);
        }
        finally
        {
            graphicsDevice.RasterizerState = previousRasterizer;
            graphicsDevice.ScissorRectangle = previousScissor;
        }

        const int titleWidth = 416;
        var titleHeight = (int)MathF.Round((float)title.Height / title.Width * titleWidth);
        var titleBounds = new Rectangle((mural.Width - titleWidth) / 2, 10, titleWidth, titleHeight);
        spriteBatch.Begin(samplerState: SamplerState.LinearClamp, transformMatrix: muralTransform);
        spriteBatch.Draw(title, titleBounds, Color.White);
        var menuBounds = new Rectangle(0, mural.Height - menu.Height, mural.Width, menu.Height);
        spriteBatch.Draw(menu, menuBounds, Color.White);
        spriteBatch.End();
    }
}
