using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace TheCards.Desktop;

/// <summary>Shows the main menu with the animated mural and title.</summary>
internal sealed class MenuScreen : IScreen
{
    private readonly GameServices services;
    private readonly Texture2D mural;
    private readonly Texture2D sideScenes;
    private readonly SideSceneEffects sideEffects;
    private readonly Texture2D title;
    private readonly Texture2D menu;
    private readonly Texture2D glow;
    private readonly Texture2D artworkMask;
    private readonly RasterizerState muralRasterizer;
    private readonly TitleReveal titleReveal;
    private readonly MainUI mainUI;
    private readonly WindowControls controls;
    private ButtonState previousButton;
    private bool previousEscape;
    private TimeSpan? shownAt;

    public MenuScreen(GameServices services)
    {
        this.services = services;
        var device = services.Device;
        controls = new WindowControls(services.Audio, isBoard: false);
        using (var stream = CardGame.OpenEmbeddedAsset("MainUI-Mural.png"))
        {
            mural = Texture2D.FromStream(device, stream);
        }
        using (var stream = CardGame.OpenEmbeddedAsset("MainUI-SideScenes.png"))
        {
            sideScenes = Texture2D.FromStream(device, stream);
        }
        using (var stream = CardGame.OpenEmbeddedAsset("MainUI-Title.png"))
        {
            title = Texture2D.FromStream(device, stream, DefaultColorProcessors.PremultiplyAlpha);
        }
        using (var stream = CardGame.OpenEmbeddedAsset("MainUI-Menu.png"))
        {
            menu = Texture2D.FromStream(device, stream, DefaultColorProcessors.PremultiplyAlpha);
        }
        glow = MuralEffects.CreateGlow(device);
        artworkMask = MuralEffects.CreateArtworkMask(mural);
        muralRasterizer = new RasterizerState { ScissorTestEnable = true };
        titleReveal = new TitleReveal(title, glow);
        sideEffects = new SideSceneEffects(sideScenes);
        mainUI = new MainUI(device, services.Batch, mural, sideScenes, sideEffects, title, menu,
            new MuralEffects(services.Batch, glow, artworkMask, mural, muralRasterizer), titleReveal, muralRasterizer);
        Activate();
    }

    public string Title => "The Cards";

    /// <summary>Prepares the menu to be shown again: replays the title reveal and ignores held input.</summary>
    public void Activate()
    {
        // Treat the button and key as held so the click or key press that opened the menu is not reused.
        previousButton = ButtonState.Pressed;
        previousEscape = true;
        shownAt = null;
    }

    public ScreenTransition Update(GameTime gameTime, Rectangle client, bool isActive)
    {
        var mouse = Mouse.GetState();
        var escape = Keyboard.GetState().IsKeyDown(Keys.Escape);
        var transition = ScreenTransition.None;
        if (isActive && escape && !previousEscape)
        {
            controls.ToggleExitPrompt();
        }
        else if (isActive && mouse.LeftButton == ButtonState.Pressed && previousButton == ButtonState.Released)
        {
            var action = controls.Click(mouse.Position, client, services.Device.Viewport);
            if (action == WindowControlResult.ConfirmedExit)
            {
                transition = ScreenTransition.ExitGame;
            }
            else if (action == WindowControlResult.Ignored && mainUI.IsSinglePlayer(mouse.Position, client))
            {
                transition = ScreenTransition.OpenBoard;
            }
        }

        previousButton = mouse.LeftButton;
        previousEscape = escape;
        return transition;
    }

    public void RequestClose() => controls.RequestExitConfirmation();

    public void Draw(GameTime gameTime)
    {
        // The animations are timed from when the menu was last shown, so the title reveal replays on return.
        shownAt ??= gameTime.TotalGameTime;
        var device = services.Device;
        device.Clear(Color.Black);
        mainUI.Draw(new GameTime(gameTime.TotalGameTime - shownAt.Value, gameTime.ElapsedGameTime));
        controls.Draw(services.Batch, services.Pixel, services.Font, device.Viewport);
    }

    public void Dispose()
    {
        muralRasterizer.Dispose();
        artworkMask.Dispose();
        glow.Dispose();
        menu.Dispose();
        titleReveal.Dispose();
        title.Dispose();
        sideEffects.Dispose();
        sideScenes.Dispose();
        mural.Dispose();
    }
}
