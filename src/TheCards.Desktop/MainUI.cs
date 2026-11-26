using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TheCards.Desktop;

/// <summary>Draws the startup screen within the game window.</summary>
internal sealed class MainUI(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, Texture2D mural,
    Texture2D sideScenes, SideSceneEffects sideEffects, Texture2D title, Texture2D menu, MuralEffects effects, TitleReveal reveal,
    RasterizerState muralRasterizer)
{
    private const int MenuItemSpacing = 258;
    private const float UltrawideAspectRatio = 21f / 9f;
    private const float SceneBlendHeightFraction = 144f / 1400f;
    private const int SceneBlendSteps = 48;

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

        var fitted = ScreenLayout.Fit(viewport, mural.Width, mural.Height);
        var useSideScenes = (float)viewport.Width / viewport.Height >= UltrawideAspectRatio &&
            fitted.Left > 0 && fitted.Right < viewport.Width;
        var destination = useSideScenes ? fitted : ScreenLayout.Cover(viewport, mural.Width, mural.Height);
        var time = (float)gameTime.TotalGameTime.TotalSeconds;
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
        var previousScissor = graphicsDevice.ScissorRectangle;
        var previousRasterizer = graphicsDevice.RasterizerState;
        graphicsDevice.ScissorRectangle = viewport.Bounds;
        try
        {
            effects.Draw(time, muralTransform);
            if (useSideScenes)
            {
                spriteBatch.Begin(samplerState: SamplerState.LinearClamp);
                DrawSideScenes(viewport, destination, time);
                spriteBatch.End();
            }
            reveal.Draw(spriteBatch, titleBounds, titleTransform, muralRasterizer, time);
        }
        finally
        {
            graphicsDevice.RasterizerState = previousRasterizer;
            graphicsDevice.ScissorRectangle = previousScissor;
        }

        DrawMenu(viewport, ScreenLayout.BottomOverlay(viewport, fitted, mural.Height, menu.Height));
    }

    private void DrawSideScenes(Viewport viewport, Rectangle muralBounds, float time)
    {
        var midpoint = sideScenes.Width / 2;
        var blendWidth = Math.Clamp((int)MathF.Round(viewport.Height * SceneBlendHeightFraction),
            1, Math.Max(1, muralBounds.Width / 2));
        const int seamMargin = 4;
        DrawSideScene(new Rectangle(0, 0, muralBounds.Left + blendWidth, viewport.Height),
            new Rectangle(0, 0, midpoint - seamMargin, sideScenes.Height), blendWidth, time, fadeOnLeft: false);
        DrawSideScene(new Rectangle(muralBounds.Right - blendWidth, 0,
                viewport.Width - muralBounds.Right + blendWidth, viewport.Height),
            new Rectangle(midpoint + seamMargin, 0, sideScenes.Width - midpoint - seamMargin, sideScenes.Height),
            blendWidth, time, fadeOnLeft: true);
    }

    private void DrawSideScene(Rectangle bounds, Rectangle source, int blendWidth, float time, bool fadeOnLeft)
    {
        var scale = Math.Max((float)bounds.Width / source.Width, (float)bounds.Height / source.Height);
        var cropWidth = Math.Clamp((int)MathF.Round(bounds.Width / scale), 2, source.Width);
        var cropHeight = Math.Clamp((int)MathF.Round(bounds.Height / scale), 1, source.Height);
        source = new Rectangle(source.X + (source.Width - cropWidth) / 2,
            source.Y + (source.Height - cropHeight) / 2, cropWidth, cropHeight);

        var fadeSourceWidth = Math.Clamp((int)MathF.Round((float)blendWidth * source.Width / bounds.Width),
            1, source.Width - 1);
        var solid = new Rectangle(bounds.X + (fadeOnLeft ? blendWidth : 0), bounds.Y,
            bounds.Width - blendWidth, bounds.Height);
        var solidSource = new Rectangle(source.X + (fadeOnLeft ? fadeSourceWidth : 0), source.Y,
            source.Width - fadeSourceWidth, source.Height);
        spriteBatch.Draw(sideScenes, solid, solidSource, Color.White);

        // Blend the added art over the original mural while keeping its effect coordinates intact.
        var fadeX = fadeOnLeft ? bounds.Left : solid.Right;
        var fadeSourceX = fadeOnLeft ? source.Left : solidSource.Right;
        var steps = Math.Min(SceneBlendSteps, Math.Min(blendWidth, fadeSourceWidth));
        for (var step = 0; step < steps; step++)
        {
            var start = step * fadeSourceWidth / steps;
            var end = (step + 1) * fadeSourceWidth / steps;
            var left = step * blendWidth / steps;
            var right = (step + 1) * blendWidth / steps;
            var progress = (step + 0.5f) / steps;
            var opacity = MathHelper.SmoothStep(0f, 1f, fadeOnLeft ? progress : 1f - progress);
            spriteBatch.Draw(sideScenes,
                new Rectangle(fadeX + left, bounds.Y, right - left, bounds.Height),
                new Rectangle(fadeSourceX + start, source.Y, end - start, source.Height), Color.White * opacity);
        }

        var scaleX = (float)solid.Width / solidSource.Width;
        var scaleY = (float)solid.Height / solidSource.Height;
        var transform = Matrix.CreateScale(scaleX, scaleY, 1) * Matrix.CreateTranslation(
            solid.X - solidSource.X * scaleX, solid.Y - solidSource.Y * scaleY, 0);
        if (fadeOnLeft)
        {
            sideEffects.DrawCityLights(spriteBatch, solidSource, transform, time);
            sideEffects.DrawGills(spriteBatch, solidSource, transform, time);
        }
        else
        {
            sideEffects.DrawForest(spriteBatch, solidSource, transform, time);
            sideEffects.DrawLeaves(spriteBatch, solidSource, transform, time);
        }
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
