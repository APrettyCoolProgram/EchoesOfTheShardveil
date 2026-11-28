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
    private readonly Texture2D titleTexture;
    private readonly Texture2D menuBar;
    private readonly Texture2D glow;
    private readonly FourWorldMuralEffects fourWorldEffects;
    private readonly RasterizerState muralRasterizer;
    private readonly TitleReveal titleReveal;
    private readonly MainUI mainUI;
    private readonly WindowControls controls;
    private readonly InputTracker input = new();
    private TimeSpan? shownAt;
    private string? contentError;

    /// <summary>Loads the menu artwork and builds the textures its animations need.</summary>
    /// <param name="services">The shared graphics and audio.</param>
    public MenuScreen(GameServices services)
    {
        this.services = services;
        var device = services.Device;
        controls = new WindowControls(services.Audio, isBoard: false);
        mural = EmbeddedAssets.LoadTexture(device, "MainUI-FourWorlds.png");
        titleTexture = EmbeddedAssets.LoadPremultipliedTexture(device, "MainUI-Title.png");
        menuBar = EmbeddedAssets.LoadPremultipliedTexture(device, "MainUI-Menu.png");
        glow = FourWorldMuralEffects.CreateGlow(device);
        muralRasterizer = new RasterizerState { ScissorTestEnable = true };
        titleReveal = new TitleReveal(titleTexture, glow);

        fourWorldEffects = new FourWorldMuralEffects(services.SpriteBatch, services.Pixel, glow, mural);
        mainUI = new MainUI(device, services.SpriteBatch, mural, titleTexture, menuBar,
            fourWorldEffects, titleReveal, muralRasterizer);
    }

    /// <inheritdoc />
    public string Title => "Echoes of the Shardveil";

    /// <summary>Prepares the menu to be shown again: replays the title reveal and ignores held input.</summary>
    public void Activate()
    {
        input.Reset();
        shownAt = null;
        contentError = null;
    }

    /// <summary>Shows a deployment error while keeping the main menu available.</summary>
    /// <param name="transition">The menu action that could not open.</param>
    public void ShowContentError(ScreenTransition transition) =>
        contentError = transition == ScreenTransition.OpenDeckbuilder
            ? "Could not open Deckbuilder."
            : "Could not start Single player.";

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
            if (action == WindowControlResult.Ignored)
            {
                return mainUI.MenuSelection(frame.Pointer, client);
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
        DrawContentError(device.Viewport);
        controls.Draw(services.SpriteBatch, services.Pixel, services.Font, device.Viewport);
    }

    /// <summary>Draws the content error above the main menu buttons.</summary>
    /// <param name="viewport">The current drawable window bounds.</param>
    private void DrawContentError(Viewport viewport)
    {
        if (contentError is not { } message || viewport.Width <= 0 || viewport.Height <= 0)
        {
            return;
        }

        var scale = Math.Min(1f, Math.Min(viewport.Width / 800f, viewport.Height / 430f));
        var fitted = ScreenLayout.Fit(viewport, mural.Width, mural.Height);
        var menuBounds = ScreenLayout.BottomOverlay(viewport, fitted, mural.Height, menuBar.Height);
        var transform = Matrix.CreateScale(scale, scale, 1) *
            Matrix.CreateTranslation((viewport.Width - 720 * scale) / 2, menuBounds.Top - 90 * scale, 0);
        var batch = services.SpriteBatch;
        batch.Begin(samplerState: SamplerState.LinearClamp, transformMatrix: transform);
        batch.Draw(services.Pixel, new Rectangle(0, 0, 720, 78), new Color(18, 26, 33));
        batch.Draw(services.Pixel, new Rectangle(0, 0, 720, 2), new Color(226, 190, 117));
        BitmapText.DrawCentered(batch, services.Font, message, 360, 8, 26, UiColors.Ivory);
        BitmapText.DrawCentered(batch, services.Font,
            "Check that the full AppData folder is beside the game, then restart.", 360, 42, 19, UiColors.Ivory);
        batch.End();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        muralRasterizer.Dispose();
        fourWorldEffects.Dispose();
        glow.Dispose();
        menuBar.Dispose();
        titleReveal.Dispose();
        titleTexture.Dispose();
        mural.Dispose();
    }
}
