using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TheCards.Desktop;

/// <summary>Projects a short burn-through and backlight animation around the original MainUI logo.</summary>
internal sealed class TitleReveal : IDisposable
{
    private const float PlaybackRate = 0.5f;

    private readonly Texture2D title;
    private readonly Texture2D glow;
    private readonly Texture2D animatedFace;
    private readonly Texture2D animatedLight;
    private readonly Texture2D logoLight;
    private readonly TitleBurn burn;
    private int previousFrame = -1;

    public TitleReveal(Texture2D title, Texture2D glow)
    {
        this.title = title;
        this.glow = glow;
        var pixels = new Color[title.Width * title.Height];
        title.GetData(pixels);
        burn = new TitleBurn(title.Width, title.Height, pixels);
        animatedFace = new Texture2D(title.GraphicsDevice, title.Width, title.Height);
        animatedLight = new Texture2D(title.GraphicsDevice, title.Width, title.Height);
        for (var i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Color(255, 255, 255, (int)pixels[i].R);
        }
        logoLight = new Texture2D(title.GraphicsDevice, title.Width, title.Height);
        logoLight.SetData(pixels);
    }

    public void Draw(SpriteBatch batch, Rectangle bounds, Matrix transform, RasterizerState rasterizer, float time)
    {
        var revealTime = time * PlaybackRate;
        var revealing = revealTime < TitleBurn.Duration;
        if (revealing)
        {
            var frame = (int)(revealTime * 30f);
            if (frame != previousFrame)
            {
                burn.Render(frame / 30f);
                animatedFace.SetData(burn.Face);
                animatedLight.SetData(burn.Light);
                previousFrame = frame;
            }

            var dark = TitleBurn.Ramp(0f, 0.43f, revealTime) * (1f - TitleBurn.Ramp(1.65f, 4.05f, revealTime));
            var shade = new Rectangle(bounds.Center.X - 480, bounds.Center.Y - 289, 960, 588);
            batch.Begin(samplerState: SamplerState.LinearClamp,
                rasterizerState: rasterizer, transformMatrix: transform);
            batch.Draw(glow, shade, Color.Black * (dark * 0.84f));
            batch.End();
        }

        batch.Begin(blendState: BlendState.Additive, samplerState: SamplerState.LinearClamp,
            rasterizerState: rasterizer, transformMatrix: transform);
        if (revealing)
        {
            DrawBacklight(batch, bounds, revealTime);
        }
        var ambient = TitleBurn.Ramp(3.05f, TitleBurn.Duration, revealTime);
        if (ambient > 0)
        {
            DrawAmbientShafts(batch, bounds, time, ambient);
        }
        batch.End();

        batch.Begin(samplerState: SamplerState.LinearClamp,
            rasterizerState: rasterizer, transformMatrix: transform);
        batch.Draw(revealing ? animatedFace : title, bounds, Color.White);
        batch.End();
    }

    private void DrawBacklight(SpriteBatch batch, Rectangle bounds, float time)
    {
        var pulse = TitleBurn.Ramp(0.16f, 1.18f, time) * (1f - TitleBurn.Ramp(2.0f, 4.12f, time));
        var flare = new Rectangle(bounds.Center.X - 264, bounds.Center.Y - 92, 528, 185);
        batch.Draw(glow, flare, new Color(100, 168, 255) { A = (byte)(37 * pulse) });

        var anchor = new Vector2(bounds.Center.X - 15f + 17f * MathF.Sin(time * 0.92f),
            bounds.Y + bounds.Height * 0.60f);
        DrawShafts(batch, animatedLight, bounds, anchor, 0.82f + 0.33f * pulse, 0.051f, time * 6f);
    }

    private void DrawAmbientShafts(SpriteBatch batch, Rectangle bounds, float time, float ambient)
    {
        var drift = time * 0.28f;
        var anchor = new Vector2(bounds.Center.X + 67f * MathF.Sin(drift),
            bounds.Y + bounds.Height * 0.54f + 16f * MathF.Sin(drift * 0.72f + 1.2f));
        var reach = 0.77f + 0.11f * MathF.Sin(drift * 0.63f);
        var strength = ambient * (0.029f + 0.005f * MathF.Sin(drift * 0.83f + 0.4f));
        DrawShafts(batch, logoLight, bounds, anchor, reach, strength, drift);
    }

    private static void DrawShafts(SpriteBatch batch, Texture2D mask, Rectangle bounds, Vector2 anchor,
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
            batch.Draw(mask, position, null, tint, 0f, Vector2.Zero,
                baseScale * zoom, SpriteEffects.None, 0f);
        }
    }

    public void Dispose()
    {
        animatedFace.Dispose();
        animatedLight.Dispose();
        logoLight.Dispose();
    }
}
