// 261127_code
// 261127_documentation

using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;

namespace EchoesOfTheShardveil.Desktop;

/// <summary>Gives access to the SDL library that MonoGame's DesktopGL platform ships with.</summary>
internal static class SdlLibrary
{
    /// <summary>Loads SDL under its name for the current operating system.</summary>
    /// <param name="library">The library handle, or zero on failure.</param>
    /// <returns><see langword="true"/> if SDL was loaded.</returns>
    public static bool TryLoad(out nint library)
    {
        var file = OperatingSystem.IsWindows() ? "SDL2.dll"
            : OperatingSystem.IsMacOS() ? "libSDL2-2.0.0.dylib" : "libSDL2-2.0.so.0";
        return NativeLibrary.TryLoad(file, typeof(Game).Assembly, null, out library);
    }

    /// <summary>Releases a handle from <see cref="TryLoad"/> once, and clears it.</summary>
    /// <param name="library">The handle to release. Zero is ignored.</param>
    public static void Free(ref nint library)
    {
        var handle = Interlocked.Exchange(ref library, 0);
        if (handle != 0)
        {
            NativeLibrary.Free(handle);
        }
    }
}
