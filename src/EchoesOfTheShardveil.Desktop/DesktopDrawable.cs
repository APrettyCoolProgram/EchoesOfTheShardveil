using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EchoesOfTheShardveil.Desktop;

/// <summary>Synchronizes the DesktopGL back buffer with the SDL window's actual pixel size.</summary>
internal sealed class DesktopDrawable : IDisposable
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void WindowSizeQuery(nint window, out int width, out int height);

    private nint library;
    private readonly WindowSizeQuery? getDrawableSize;
    private readonly WindowSizeQuery? getClientSize;

    public DesktopDrawable()
    {
        var file = OperatingSystem.IsWindows() ? "SDL2.dll"
            : OperatingSystem.IsMacOS() ? "libSDL2-2.0.0.dylib" : "libSDL2-2.0.so.0";
        if (NativeLibrary.TryLoad(file, typeof(Game).Assembly, null, out library))
        {
            getDrawableSize = LoadQuery("SDL_GL_GetDrawableSize");
            getClientSize = LoadQuery("SDL_GetWindowSize");
        }
    }

    private WindowSizeQuery? LoadQuery(string name) => NativeLibrary.TryGetExport(library, name, out var function)
        ? Marshal.GetDelegateForFunctionPointer<WindowSizeQuery>(function)
        : null;

    public Rectangle Synchronize(GraphicsDevice device, GameWindow window)
    {
        var client = window.ClientBounds;
        if (window.Handle == 0)
        {
            return client;
        }

        if (getClientSize is not null)
        {
            getClientSize(window.Handle, out var clientWidth, out var clientHeight);
            if (clientWidth > 0 && clientHeight > 0)
            {
                client.Width = clientWidth;
                client.Height = clientHeight;
            }
        }

        if (getDrawableSize is null)
        {
            return client;
        }

        getDrawableSize(window.Handle, out var width, out var height);
        var presentation = device.PresentationParameters;
        var viewport = device.Viewport;
        if (width <= 0 || height <= 0
            || (width == presentation.BackBufferWidth && height == presentation.BackBufferHeight
                && width == viewport.Width && height == viewport.Height))
        {
            return client;
        }

        // DesktopGL uses the back-buffer height to convert both viewport and scissor coordinates.
        presentation.BackBufferWidth = width;
        presentation.BackBufferHeight = height;
        device.Viewport = new Viewport(0, 0, width, height);
        device.ScissorRectangle = device.Viewport.Bounds;
        return client;
    }

    public void Dispose()
    {
        var handle = Interlocked.Exchange(ref library, 0);
        if (handle != 0)
        {
            NativeLibrary.Free(handle);
        }
    }
}
