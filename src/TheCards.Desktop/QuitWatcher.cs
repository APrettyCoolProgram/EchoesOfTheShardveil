using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;

namespace TheCards.Desktop;

/// <summary>
/// Tells an application quit (the macOS menu's Quit or Cmd+Q) apart from the window's close button.
/// SDL reports a quit request on its own, but a close button first reports a window close, then the quit.
/// </summary>
internal sealed class QuitWatcher : IDisposable
{
    private const int QuitEvent = 0x100;
    private const int WindowEvent = 0x200;
    private const byte WindowClose = 14;
    // SDL_Event layout: type (uint32), timestamp (uint32), windowID (uint32), then the window event id (byte).
    private const int WindowEventIdOffset = 12;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int EventWatch(nint userdata, nint sdlEvent);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void WatchRegistration(nint filter, nint userdata);

    private nint library;
    private readonly EventWatch? watch;
    private readonly WatchRegistration? remove;
    private readonly nint filter;
    private volatile bool windowCloseSeen;
    private volatile bool applicationQuit;

    public QuitWatcher()
    {
        var file = OperatingSystem.IsWindows() ? "SDL2.dll"
            : OperatingSystem.IsMacOS() ? "libSDL2-2.0.0.dylib" : "libSDL2-2.0.so.0";
        if (!NativeLibrary.TryLoad(file, typeof(Game).Assembly, null, out library) ||
            !NativeLibrary.TryGetExport(library, "SDL_AddEventWatch", out var add) ||
            !NativeLibrary.TryGetExport(library, "SDL_DelEventWatch", out var delete))
        {
            return;
        }

        watch = OnEvent;
        filter = Marshal.GetFunctionPointerForDelegate(watch);
        remove = Marshal.GetDelegateForFunctionPointer<WatchRegistration>(delete);
        Marshal.GetDelegateForFunctionPointer<WatchRegistration>(add)(filter, 0);
    }

    private int OnEvent(nint userdata, nint sdlEvent)
    {
        var type = Marshal.ReadInt32(sdlEvent);
        if (type == WindowEvent && Marshal.ReadByte(sdlEvent, WindowEventIdOffset) == WindowClose)
        {
            windowCloseSeen = true;
        }
        else if (type == QuitEvent)
        {
            // A quit that no window close came before is the application being quit from elsewhere.
            applicationQuit = !windowCloseSeen;
            windowCloseSeen = false;
        }
        return 1;
    }

    /// <summary>Returns whether the latest exit request was an application quit, and clears it.</summary>
    public bool TakeApplicationQuit()
    {
        var quit = applicationQuit;
        applicationQuit = false;
        return quit;
    }

    public void Dispose()
    {
        if (remove is not null)
        {
            remove(filter, 0);
        }
        var handle = Interlocked.Exchange(ref library, 0);
        if (handle != 0)
        {
            NativeLibrary.Free(handle);
        }
    }
}
