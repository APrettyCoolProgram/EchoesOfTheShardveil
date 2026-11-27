// 261127_code
// 261127_documentation

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EchoesOfTheShardveil.Desktop;

/// <summary>The single MonoGame game. It owns the window and shared resources and switches between screens.</summary>
internal sealed class CardGame : Game
{
    private readonly GraphicsDeviceManager graphics;
    private readonly GameAudio audio = new();
    private GameServices? services;
    private DesktopDrawable? drawable;
    private QuitWatcher? quitWatcher;
    private MenuScreen? menu;
    private BoardScreen? board;
    private bool exitConfirmed;

    /// <summary>Initializes the game with a resizable window at the board's design size.</summary>
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

    /// <summary>Gets the screen being shown: the board during a duel, otherwise the menu.</summary>
    private IScreen? CurrentScreen => (IScreen?)board ?? menu;

    protected override void LoadContent()
    {
        var spriteBatch = new SpriteBatch(GraphicsDevice);
        var pixel = new Texture2D(GraphicsDevice, 1, 1);
        pixel.SetData([Color.White]);
        var font = EmbeddedAssets.LoadPremultipliedTexture(GraphicsDevice, "Board-Font.png");
        services = new GameServices(GraphicsDevice, spriteBatch, pixel, font, audio);
        drawable = new DesktopDrawable();
        quitWatcher = new QuitWatcher();
        menu = new MenuScreen(services);
        Window.Title = menu.Title;
        audio.Start();
    }

    protected override void Update(GameTime gameTime)
    {
        if (CurrentScreen is { } screen)
        {
            var client = drawable?.Synchronize(GraphicsDevice, Window) ?? Window.ClientBounds;
            Apply(screen.Update(gameTime, client, IsActive));
        }
        base.Update(gameTime);
    }

    protected override void OnExiting(object sender, ExitingEventArgs args)
    {
        base.OnExiting(sender, args);
        // Quitting the application (such as from the macOS menu bar) exits at once.
        if (quitWatcher?.TakeApplicationQuit() == true)
        {
            return;
        }

        // The window's close button asks first: the menu offers to quit, the board offers to forfeit.
        if (!exitConfirmed && CurrentScreen is { } screen)
        {
            args.Cancel = true;
            screen.RequestClose();
        }
    }

    protected override void Draw(GameTime gameTime)
    {
        CurrentScreen?.Draw(gameTime);
        base.Draw(gameTime);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            board?.Dispose();
            menu?.Dispose();
            audio.Dispose();
            services?.Font.Dispose();
            services?.Pixel.Dispose();
            services?.SpriteBatch.Dispose();
            quitWatcher?.Dispose();
            drawable?.Dispose();
        }
        base.Dispose(disposing);
    }

    /// <summary>Carries out what a screen asked for.</summary>
    /// <param name="transition">The requested change.</param>
    private void Apply(ScreenTransition transition)
    {
        switch (transition)
        {
            case ScreenTransition.OpenBoard when services is not null:
                board = new BoardScreen(services);
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
}
