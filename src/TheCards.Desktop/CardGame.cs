using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace TheCards.Desktop;

internal sealed class CardGame : Game
{
    private readonly GraphicsDeviceManager graphics;
    private SpriteBatch? spriteBatch;
    private Texture2D? mural;
    private Texture2D? glow;
    private Texture2D? artworkMask;
    private SoundEffect? soundtrack;
    private SoundEffectInstance? music;
    private MainUI? mainUI;

    public CardGame()
    {
        graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 1280,
            PreferredBackBufferHeight = 720
        };

        Content.RootDirectory = "Content";
        Window.Title = "The Cards";
        Window.AllowUserResizing = true;
        IsMouseVisible = true;
    }

    protected override void LoadContent()
    {
        using var stream = OpenEmbeddedAsset("MainUI-Mural.png");
        mural = Texture2D.FromStream(GraphicsDevice, stream);
        spriteBatch = new SpriteBatch(GraphicsDevice);
        glow = MuralEffects.CreateGlow(GraphicsDevice);
        artworkMask = MuralEffects.CreateArtworkMask(mural);
        mainUI = new MainUI(GraphicsDevice, spriteBatch, mural,
            new MuralEffects(spriteBatch, glow, artworkMask, mural));

        try
        {
            using var musicStream = OpenEmbeddedAsset("MainUI-Theme.wav");
            soundtrack = SoundEffect.FromStream(musicStream);
            music = soundtrack.CreateInstance();
            music.IsLooped = true;
            music.Volume = 0.35f;
            music.Play();
        }
        catch (NoAudioHardwareException)
        {
            // The startup screen can still run without an audio device.
        }
    }

    private static Stream OpenEmbeddedAsset(string name) =>
        typeof(CardGame).Assembly.GetManifestResourceStream(name)
        ?? throw new InvalidOperationException($"Missing embedded game asset '{name}'.");

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            music?.Dispose();
            soundtrack?.Dispose();
            spriteBatch?.Dispose();
            artworkMask?.Dispose();
            glow?.Dispose();
            mural?.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override void Update(GameTime gameTime)
    {
        if (Keyboard.GetState().IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        mainUI?.Draw(gameTime);
        base.Draw(gameTime);
    }
}
