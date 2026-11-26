using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace TheCards.Desktop;

/// <summary>Hosts one single-player duel. A new screen is created for each visit from the menu.</summary>
internal sealed class BoardScreen : IScreen
{
    private readonly GameServices services;
    private readonly WindowControls controls;
    private readonly Texture2D heroPortrait;
    private readonly Texture2D heroHealth;
    private readonly Texture2D heroDropRing;
    private readonly RasterizerState boardRasterizer;
    private readonly BoardUI board;
    // Start as held so the click that opened the board is not treated as a board click.
    private ButtonState previousButton = ButtonState.Pressed;
    private ButtonState previousRightButton = ButtonState.Pressed;
    private bool previousEscape = true;
    private int previousScroll = Mouse.GetState().ScrollWheelValue;

    public BoardScreen(GameServices services)
    {
        this.services = services;
        var device = services.Device;
        controls = new WindowControls(services.Audio, isBoard: true);
        boardRasterizer = new RasterizerState { ScissorTestEnable = true };
        using (var stream = CardGame.OpenEmbeddedAsset("Board-Hero-Knight.png"))
        using (var portrait = Texture2D.FromStream(device, stream, DefaultColorProcessors.PremultiplyAlpha))
        {
            heroPortrait = CircleTexture.FramePortrait(portrait, BoardUI.HeroPortraitSize, borderWidth: 2);
        }
        using (var stream = CardGame.OpenEmbeddedAsset("Board-Hero-Health.png"))
        {
            heroHealth = Texture2D.FromStream(device, stream, DefaultColorProcessors.PremultiplyAlpha);
        }
        heroDropRing = CircleTexture.Create(device, BoardUI.HeroPortraitSize + 6, borderWidth: 3, Color.Transparent);
        board = new BoardUI(services.Batch, services.Pixel, services.Font, heroPortrait, heroHealth, heroDropRing);
    }

    public string Title => "The Cards - Single player";

    public ScreenTransition Update(GameTime gameTime, Rectangle client, bool isActive)
    {
        var mouse = Mouse.GetState();
        var escape = Keyboard.GetState().IsKeyDown(Keys.Escape);
        var transition = ScreenTransition.None;
        if (isActive)
        {
            var viewport = services.Device.Viewport;
            var view = new BoardViewport(viewport);
            board.SetLayout(view);

            var action = WindowControlResult.Ignored;
            if (escape && !previousEscape)
            {
                controls.ToggleExitPrompt();
                action = WindowControlResult.Handled;
            }
            else if (mouse.LeftButton == ButtonState.Pressed && previousButton == ButtonState.Released)
            {
                action = controls.Click(mouse.Position, client, viewport);
            }

            if (action == WindowControlResult.ConfirmedExit)
            {
                transition = ScreenTransition.ReturnToMenu;
            }
            else if (!controls.IsConfirming)
            {
                Point? pointer = null;
                if (view.TryMap(mouse.Position, client, out var position))
                {
                    pointer = position;
                    if (action == WindowControlResult.Ignored &&
                        mouse.LeftButton == ButtonState.Pressed && previousButton == ButtonState.Released)
                    {
                        board.PointerPressed(position);
                    }
                    if (mouse.RightButton == ButtonState.Pressed && previousRightButton == ButtonState.Released)
                    {
                        board.RightClick(position);
                    }
                    if (mouse.ScrollWheelValue != previousScroll)
                    {
                        board.ScrollInspector(position, previousScroll.CompareTo(mouse.ScrollWheelValue));
                    }
                }
                if (mouse.LeftButton == ButtonState.Released && previousButton == ButtonState.Pressed)
                {
                    board.PointerReleased(pointer);
                }
                board.Update(gameTime.ElapsedGameTime, pointer);
            }
            else
            {
                board.CancelDrag();
            }
        }
        else
        {
            board.CancelDrag();
        }

        previousRightButton = mouse.RightButton;
        previousButton = mouse.LeftButton;
        previousEscape = escape;
        previousScroll = mouse.ScrollWheelValue;
        return transition;
    }

    public void RequestClose() => controls.RequestExitConfirmation();

    public void Draw(GameTime gameTime)
    {
        var device = services.Device;
        device.Clear(BoardUI.BackgroundColor);
        var viewport = device.Viewport;
        var view = new BoardViewport(viewport);
        if (view.IsDrawable)
        {
            var previousScissor = device.ScissorRectangle;
            var previousRasterizer = device.RasterizerState;
            device.ScissorRectangle = viewport.Bounds;
            try
            {
                board.Draw(view, boardRasterizer);
            }
            finally
            {
                device.RasterizerState = previousRasterizer;
                device.ScissorRectangle = previousScissor;
            }
        }
        controls.Draw(services.Batch, services.Pixel, services.Font, viewport);
    }

    public void Dispose()
    {
        heroPortrait.Dispose();
        heroHealth.Dispose();
        heroDropRing.Dispose();
        boardRasterizer.Dispose();
    }
}
