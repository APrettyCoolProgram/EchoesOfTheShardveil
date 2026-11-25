using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TheCards.Desktop;

/// <summary>Draws the startup screen within the game window.</summary>
internal sealed class MainUI(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, Texture2D mural, MuralEffects effects)
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
        effects.Draw((float)gameTime.TotalGameTime.TotalSeconds, muralTransform);
    }
}
