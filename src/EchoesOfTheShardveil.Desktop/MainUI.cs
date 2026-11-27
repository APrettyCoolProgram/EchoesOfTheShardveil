using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EchoesOfTheShardveil.Desktop;

/// <summary>Draws the startup screen within the game window.</summary>
/// <param name="graphicsDevice">The graphics device.</param>
/// <param name="spriteBatch">The sprite batch shared with the effects.</param>
/// <param name="mural">The main painting.</param>
/// <param name="sideScenes">The two side panels for ultrawide windows, side by side in one texture.</param>
/// <param name="sideEffects">The animations on the side panels.</param>
/// <param name="titleTexture">The wordmark artwork.</param>
/// <param name="menuBar">The menu artwork along the bottom.</param>
/// <param name="muralEffects">The animations on the mural.</param>
/// <param name="titleReveal">The title animation.</param>
/// <param name="muralRasterizer">A rasterizer state with scissoring enabled.</param>
internal sealed class MainUI(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, Texture2D mural,
    Texture2D sideScenes, SideSceneEffects sideEffects, Texture2D titleTexture, Texture2D menuBar,
    MuralEffects muralEffects, TitleReveal titleReveal, RasterizerState muralRasterizer)
{
    private const int MenuItemSpacing = 258;
    private const float UltrawideAspectRatio = 21f / 9f;
    private const float SceneBlendHeightFraction = 144f / 1400f;
    private const int SceneBlendSteps = 48;
    private const float TitleScale = 0.7425f;

    /// <summary>Tests the Single player label in the bottom-anchored menu.</summary>
    /// <param name="mouse">The mouse position in client units.</param>
    /// <param name="clientBounds">The window's client size in mouse units.</param>
    /// <returns><see langword="true"/> if the mouse is over the label.</returns>
    public bool IsOverSinglePlayer(Point mouse, Rectangle clientBounds)
    {
        var viewport = graphicsDevice.Viewport;
        var fitted = ScreenLayout.Fit(viewport, mural.Width, mural.Height);
        var menuBounds = ScreenLayout.BottomOverlay(viewport, fitted, mural.Height, menuBar.Height);
        // The label's position is in the menu artwork's own pixels.
        return ScreenLayout.TryMap(mouse, clientBounds, viewport, menuBounds,
            menuBar.Width, menuBar.Height, out var position)
            && new Rectangle((menuBar.Width + MenuItemSpacing) / 2 - 120, 24, 240, 78)
                .Contains(position);
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
            if (useSideScenes)
            {
                spriteBatch.Begin(samplerState: SamplerState.LinearClamp);
                DrawSideScenes(viewport, destination, time);
                spriteBatch.End();
            }
            titleReveal.Draw(spriteBatch, titleBounds, titleTransform, muralRasterizer, time);
        }
        finally
        {
            graphicsDevice.RasterizerState = previousRasterizer;
            graphicsDevice.ScissorRectangle = previousScissor;
        }

        DrawMenu(viewport, ScreenLayout.BottomOverlay(viewport, fitted, mural.Height, menuBar.Height));
    }

    /// <summary>Draws and animates the two ultrawide side panels, overlapping the mural.</summary>
    /// <param name="viewport">The window viewport.</param>
    /// <param name="muralBounds">Where the mural was drawn.</param>
    /// <param name="time">Seconds since the menu was shown.</param>
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

    /// <summary>Draws one side panel, fading the edge that meets the mural.</summary>
    /// <param name="bounds">The panel's screen area.</param>
    /// <param name="source">The panel's area of the side artwork.</param>
    /// <param name="blendWidth">The width of the fade on screen.</param>
    /// <param name="time">Seconds since the menu was shown.</param>
    /// <param name="fadeOnLeft">
    /// <see langword="true"/> for the right panel; <see langword="false"/> for the left panel.
    /// </param>
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
        // The right panel fades on its left edge, and the left panel on its right edge.
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
