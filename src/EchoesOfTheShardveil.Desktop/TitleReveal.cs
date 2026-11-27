using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EchoesOfTheShardveil.Desktop;

/// <summary>Projects a short burn-through and backlight animation around the original MainUI logo.</summary>
internal sealed class TitleReveal : IDisposable
{
    private const float PlaybackRate = 0.5f;
    private const float FramesPerSecond = 30f;

    private readonly Texture2D wordmark;
    private readonly Texture2D glow;
    private readonly Texture2D animatedFace;
    private readonly Texture2D animatedLight;
    private readonly Texture2D wordmarkMask;
    private readonly TitleBurn burn;
    private int previousFrame = -1;

    /// <summary>Creates the textures the animation draws from the wordmark.</summary>
    /// <param name="wordmark">The title artwork, with premultiplied alpha. The caller keeps ownership.</param>
    /// <param name="glow">A soft light sprite. The caller keeps ownership.</param>
    public TitleReveal(Texture2D wordmark, Texture2D glow)
    {
        this.wordmark = wordmark;
        this.glow = glow;
        var pixels = new Color[wordmark.Width * wordmark.Height];
        wordmark.GetData(pixels);
        burn = new TitleBurn(wordmark.Width, wordmark.Height, pixels);
        animatedFace = new Texture2D(wordmark.GraphicsDevice, wordmark.Width, wordmark.Height);
        animatedLight = new Texture2D(wordmark.GraphicsDevice, wordmark.Width, wordmark.Height);
        for (var i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Color(255, 255, 255, (int)pixels[i].R);
        }
        wordmarkMask = TextureFactory.FromPixels(wordmark.GraphicsDevice, wordmark.Width, wordmark.Height, pixels);
    }

    /// <summary>Draws the title with its reveal, then its slowly drifting shafts of light.</summary>
    /// <param name="spriteBatch">A sprite batch that has not begun.</param>
    /// <param name="bounds">Where the title goes, before the transform.</param>
    /// <param name="transform">The transform from title coordinates to the screen.</param>
    /// <param name="rasterizer">The rasterizer state to draw with.</param>
    /// <param name="time">Seconds since the menu was shown.</param>
    public void Draw(SpriteBatch spriteBatch, Rectangle bounds, Matrix transform, RasterizerState rasterizer, float time)
    {
        var revealTime = time * PlaybackRate;
        var revealing = revealTime < TitleBurn.Duration;
        if (revealing)
        {
            // Rebuilding the pixels is costly, so the textures only change when the animation frame does.
            var frame = (int)(revealTime * FramesPerSecond);
            if (frame != previousFrame)
            {
                burn.Render(frame / FramesPerSecond);
                animatedFace.SetData(burn.Face);
                animatedLight.SetData(burn.Light);
                previousFrame = frame;
            }

            var dark = TitleBurn.Ramp(0f, 0.43f, revealTime) * (1f - TitleBurn.Ramp(1.65f, 4.05f, revealTime));
            var shadeWidth = Math.Max(960, bounds.Width + 544);
            var shade = new Rectangle(bounds.Center.X - shadeWidth / 2, bounds.Center.Y - 289, shadeWidth, 588);
            spriteBatch.Begin(samplerState: SamplerState.LinearClamp,
                rasterizerState: rasterizer, transformMatrix: transform);
            spriteBatch.Draw(glow, shade, Color.Black * (dark * 0.84f));
            spriteBatch.End();
        }

        spriteBatch.Begin(blendState: BlendState.Additive, samplerState: SamplerState.LinearClamp,
            rasterizerState: rasterizer, transformMatrix: transform);
        if (revealing)
        {
            DrawBacklight(spriteBatch, bounds, revealTime);
        }
        var ambient = TitleBurn.Ramp(3.05f, TitleBurn.Duration, revealTime);
        if (ambient > 0)
        {
            DrawAmbientShafts(spriteBatch, bounds, time, ambient);
        }
        spriteBatch.End();

        spriteBatch.Begin(samplerState: SamplerState.LinearClamp,
            rasterizerState: rasterizer, transformMatrix: transform);
        spriteBatch.Draw(revealing ? animatedFace : wordmark, bounds, Color.White);
        spriteBatch.End();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        animatedFace.Dispose();
        animatedLight.Dispose();
        wordmarkMask.Dispose();
    }

    /// <summary>Draws the flare and shafts that accompany the burn-through.</summary>
    /// <param name="spriteBatch">A sprite batch drawing additively.</param>
    /// <param name="bounds">The title bounds.</param>
    /// <param name="time">Animation seconds since the reveal began.</param>
    private void DrawBacklight(SpriteBatch spriteBatch, Rectangle bounds, float time)
    {
        var pulse = TitleBurn.Ramp(0.16f, 1.18f, time) * (1f - TitleBurn.Ramp(2.0f, 4.12f, time));
        var flareWidth = bounds.Width + 112;
        var flare = new Rectangle(bounds.Center.X - flareWidth / 2, bounds.Center.Y - 92, flareWidth, 185);
        spriteBatch.Draw(glow, flare, new Color(100, 168, 255) { A = (byte)(37 * pulse) });

        var anchor = new Vector2(bounds.Center.X - 15f + 17f * MathF.Sin(time * 0.92f),
            bounds.Y + bounds.Height * 0.60f);
        DrawShafts(spriteBatch, animatedLight, bounds, anchor, 0.82f + 0.33f * pulse, 0.051f, time * 6f);
    }

    /// <summary>Draws the faint shafts that keep drifting after the reveal.</summary>
    /// <param name="spriteBatch">A sprite batch drawing additively.</param>
    /// <param name="bounds">The title bounds.</param>
    /// <param name="time">Seconds since the menu was shown.</param>
    /// <param name="ambient">How far the effect has faded in.</param>
    private void DrawAmbientShafts(SpriteBatch spriteBatch, Rectangle bounds, float time, float ambient)
    {
        var drift = time * 0.28f;
        var anchor = new Vector2(bounds.Center.X + 67f * MathF.Sin(drift),
            bounds.Y + bounds.Height * 0.54f + 16f * MathF.Sin(drift * 0.72f + 1.2f));
        var reach = 0.77f + 0.11f * MathF.Sin(drift * 0.63f);
        var strength = ambient * (0.029f + 0.005f * MathF.Sin(drift * 0.83f + 0.4f));
        DrawShafts(spriteBatch, wordmarkMask, bounds, anchor, reach, strength, drift);
    }

    /// <summary>
    /// Draws light shafts as faint, progressively enlarged copies of a mask, scaled away from an anchor.
    /// </summary>
    /// <param name="spriteBatch">A sprite batch drawing additively.</param>
    /// <param name="mask">The shape casting the light.</param>
    /// <param name="bounds">The title bounds.</param>
    /// <param name="anchor">Where the light appears to come from.</param>
    /// <param name="reach">How far the shafts extend.</param>
    /// <param name="intensity">The strength.</param>
    /// <param name="phase">Animates the variation along the shafts.</param>
    private static void DrawShafts(SpriteBatch spriteBatch, Texture2D mask, Rectangle bounds, Vector2 anchor,
        float reach, float intensity, float phase)
    {
        var topLeft = new Vector2(bounds.X, bounds.Y);
        var baseScale = new Vector2((float)bounds.Width / mask.Width, (float)bounds.Height / mask.Height);
        const int samples = 27;
        for (var i = samples; i >= 1; i--)
        {
            var travel = i / (float)samples;
            var zoom = 1f + travel * reach;
            var position = anchor + (topLeft - anchor) * zoom;
            var textureVariation = 1f + 0.14f * MathF.Sin(travel * 34f - phase);
            var strength = (1f - travel) * intensity * textureVariation;
            var tint = new Color(135, 194, 255) { A = (byte)MathF.Round(255f * strength) };
            spriteBatch.Draw(mask, position, null, tint, 0f, Vector2.Zero,
                baseScale * zoom, SpriteEffects.None, 0f);
        }
    }
}
