// 261127_code
// 261127_documentation

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace EchoesOfTheShardveil.Desktop;

/// <summary>What changed in the mouse and keyboard since the previous frame.</summary>
/// <param name="Pointer">The mouse position in window client units.</param>
/// <param name="LeftPressed">Whether the left button went down.</param>
/// <param name="LeftReleased">Whether the left button came up.</param>
/// <param name="RightPressed">Whether the right button went down.</param>
/// <param name="EscapePressed">Whether the Escape key went down.</param>
/// <param name="ScrollDirection">1 when the wheel scrolled down, -1 when it scrolled up, otherwise 0.</param>
internal readonly record struct InputFrame(Point Pointer, bool LeftPressed, bool LeftReleased, bool RightPressed,
    bool EscapePressed, int ScrollDirection);

/// <summary>Turns the polled mouse and keyboard state into one-frame events.</summary>
internal sealed class InputTracker
{
    private ButtonState previousLeft;
    private ButtonState previousRight;
    private bool previousEscape;
    private int previousScroll;

    /// <summary>Initializes a tracker that ignores any input already being held.</summary>
    public InputTracker() => Reset();

    /// <summary>
    /// Treats the buttons and Escape as held, so the click or key press that opened a screen is not reused.
    /// </summary>
    public void Reset()
    {
        previousLeft = ButtonState.Pressed;
        previousRight = ButtonState.Pressed;
        previousEscape = true;
        previousScroll = Mouse.GetState().ScrollWheelValue;
    }

    /// <summary>Reads the devices and compares them with the previous call. Call once per frame.</summary>
    /// <returns>The events for this frame.</returns>
    public InputFrame Poll()
    {
        var mouse = Mouse.GetState();
        var escape = Keyboard.GetState().IsKeyDown(Keys.Escape);
        var frame = new InputFrame(
            mouse.Position,
            mouse.LeftButton == ButtonState.Pressed && previousLeft == ButtonState.Released,
            mouse.LeftButton == ButtonState.Released && previousLeft == ButtonState.Pressed,
            mouse.RightButton == ButtonState.Pressed && previousRight == ButtonState.Released,
            escape && !previousEscape,
            previousScroll.CompareTo(mouse.ScrollWheelValue));
        previousLeft = mouse.LeftButton;
        previousRight = mouse.RightButton;
        previousEscape = escape;
        previousScroll = mouse.ScrollWheelValue;
        return frame;
    }
}
