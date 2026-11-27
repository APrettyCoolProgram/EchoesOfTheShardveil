// 261127_code
// 261127_documentation

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EchoesOfTheShardveil.Desktop;

/// <summary>Shows the main menu with the animated mural and title.</summary>
internal sealed class MenuScreen : IScreen
{
    private readonly GameServices services;
    private readonly Texture2D mural;
    private readonly Texture2D sideScenes;
    private readonly SideSceneEffects sideEffects;
    private readonly Texture2D titleTexture;
    private readonly Texture2D menuBar;
    private readonly Texture2D glow;
    private readonly Texture2D artworkMask;
    private readonly RasterizerState muralRasterizer;
    private readonly TitleReveal titleReveal;
    private readonly MainUI mainUI;
    private readonly WindowControls controls;
    private readonly InputTracker input = new();
    private TimeSpan? shownAt;

    /// <summary>Loads the menu artwork and builds the textures its animations need.</summary>
    /// <param name="services">The shared graphics and audio.</param>
    public MenuScreen(GameServices services)
    {
        this.services = services;
        var device = services.Device;
        controls = new WindowControls(services.Audio, isBoard: false);
        mural = EmbeddedAssets.LoadTexture(device, "MainUI-Mural.png");
        sideScenes = EmbeddedAssets.LoadTexture(device, "MainUI-SideScenes.png");
        titleTexture = EmbeddedAssets.LoadPremultipliedTexture(device, "MainUI-Title.png");
        menuBar = EmbeddedAssets.LoadPremultipliedTexture(device, "MainUI-Menu.png");
        glow = MuralEffects.CreateGlow(device);
        artworkMask = MuralEffects.CreateArtworkMask(mural);
        muralRasterizer = new RasterizerState { ScissorTestEnable = true };
        titleReveal = new TitleReveal(titleTexture, glow);
        sideEffects = new SideSceneEffects(sideScenes);
        mainUI = new MainUI(device, services.SpriteBatch, mural, sideScenes, sideEffects, titleTexture, menuBar,
            new MuralEffects(services.SpriteBatch, glow, artworkMask, mural, muralRasterizer), titleReveal,
            muralRasterizer);
    }

    /// <inheritdoc />
    public string Title => "Echoes of the Shardveil";

    /// <summary>Prepares the menu to be shown again: replays the title reveal and ignores held input.</summary>
    public void Activate()
    {
        input.Reset();
        shownAt = null;
    }

    /// <inheritdoc />
    public ScreenTransition Update(GameTime gameTime, Rectangle client, bool isActive)
    {
        var frame = input.Poll();
        if (!isActive)
        {
            return ScreenTransition.None;
        }

        if (frame.EscapePressed)
        {
            controls.ToggleExitPrompt();
        }
        else if (frame.LeftPressed)
        {
            var action = controls.Click(frame.Pointer, client, services.Device.Viewport);
            if (action == WindowControlResult.ConfirmedExit)
            {
                return ScreenTransition.ExitGame;
            }
            if (action == WindowControlResult.Ignored && mainUI.IsOverSinglePlayer(frame.Pointer, client))
            {
                return ScreenTransition.OpenBoard;
            }
        }

        return ScreenTransition.None;
    }

    /// <inheritdoc />
    public void RequestClose() => controls.RequestExitConfirmation();

    /// <inheritdoc />
    public void Draw(GameTime gameTime)
    {
        // The animations are timed from when the menu was last shown, so the title reveal replays on return.
        shownAt ??= gameTime.TotalGameTime;
        var device = services.Device;
        device.Clear(Color.Black);
        mainUI.Draw(new GameTime(gameTime.TotalGameTime - shownAt.Value, gameTime.ElapsedGameTime));
        controls.Draw(services.SpriteBatch, services.Pixel, services.Font, device.Viewport);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        muralRasterizer.Dispose();
        artworkMask.Dispose();
        glow.Dispose();
        menuBar.Dispose();
        titleReveal.Dispose();
        titleTexture.Dispose();
        sideEffects.Dispose();
        sideScenes.Dispose();
        mural.Dispose();
    }
}
