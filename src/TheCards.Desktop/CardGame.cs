using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TheCards.Desktop;

/// <summary>The single MonoGame game. It owns the window and shared resources and switches between screens.</summary>
internal sealed class CardGame : Game
{
    private readonly GraphicsDeviceManager graphics;
    private readonly GameAudio audio = new();
    private SpriteBatch? batch;
    private Texture2D? pixel;
    private Texture2D? font;
    private DesktopDrawable? drawable;
    private MenuScreen? menu;
    private BoardScreen? board;
    private bool exitConfirmed;

    public CardGame()
    {
        graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = BoardUI.Width,
            PreferredBackBufferHeight = BoardUI.Height,
            HardwareModeSwitch = false,
        };
        Window.AllowUserResizing = true;
        IsMouseVisible = true;
    }

    private IScreen? Current => (IScreen?)board ?? menu;

    internal static Stream OpenEmbeddedAsset(string name) =>
        typeof(CardGame).Assembly.GetManifestResourceStream(name)
        ?? throw new InvalidOperationException($"Missing embedded game asset '{name}'.");

    protected override void LoadContent()
    {
        batch = new SpriteBatch(GraphicsDevice);
        pixel = new Texture2D(GraphicsDevice, 1, 1);
        pixel.SetData([Color.White]);
        using (var stream = OpenEmbeddedAsset("Board-Font.png"))
        {
            font = Texture2D.FromStream(GraphicsDevice, stream, DefaultColorProcessors.PremultiplyAlpha);
        }
        drawable = new DesktopDrawable();
        menu = new MenuScreen(new GameServices(GraphicsDevice, batch, pixel, font, audio));
        Window.Title = menu.Title;
        audio.Start();
    }

    protected override void Update(GameTime gameTime)
    {
        if (Current is { } screen)
        {
            var client = drawable?.Synchronize(GraphicsDevice, Window) ?? Window.ClientBounds;
            Apply(screen.Update(gameTime, client, IsActive));
        }
        base.Update(gameTime);
    }

    private void Apply(ScreenTransition transition)
    {
        switch (transition)
        {
            case ScreenTransition.OpenBoard when batch is not null && pixel is not null && font is not null:
                board = new BoardScreen(new GameServices(GraphicsDevice, batch, pixel, font, audio));
                Window.Title = board.Title;
                break;
            case ScreenTransition.ReturnToMenu when board is not null && menu is not null:
                board.Dispose();
                board = null;
                menu.Activate();
                Window.Title = menu.Title;
                break;
            case ScreenTransition.ExitGame:
                exitConfirmed = true;
                Exit();
                break;
        }
    }

    protected override void OnExiting(object sender, ExitingEventArgs args)
    {
        base.OnExiting(sender, args);
        // The window's close button asks first: the menu offers to quit, the board offers to forfeit.
        if (!exitConfirmed && Current is { } screen)
        {
            args.Cancel = true;
            screen.RequestClose();
        }
    }

    protected override void Draw(GameTime gameTime)
    {
        Current?.Draw(gameTime);
        base.Draw(gameTime);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            board?.Dispose();
            menu?.Dispose();
            audio.Dispose();
            font?.Dispose();
            pixel?.Dispose();
            batch?.Dispose();
            drawable?.Dispose();
        }
        base.Dispose(disposing);
    }
}
