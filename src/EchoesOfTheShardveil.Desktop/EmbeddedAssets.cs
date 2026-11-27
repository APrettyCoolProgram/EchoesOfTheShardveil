// 261127_code
// 261127_documentation

using Microsoft.Xna.Framework.Graphics;

namespace EchoesOfTheShardveil.Desktop;

/// <summary>Loads the art and audio embedded in the game assembly.</summary>
internal static class EmbeddedAssets
{
    /// <summary>Opens an embedded asset for reading.</summary>
    /// <param name="name">The logical resource name, such as <c>Board-Font.png</c>.</param>
    /// <returns>A stream the caller must dispose.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no asset has that name.</exception>
    public static Stream Open(string name) =>
        typeof(EmbeddedAssets).Assembly.GetManifestResourceStream(name)
        ?? throw new InvalidOperationException($"Missing embedded game asset '{name}'.");

    /// <summary>Loads an embedded image with its colors unchanged, which suits opaque artwork.</summary>
    /// <param name="device">The graphics device that will own the texture.</param>
    /// <param name="name">The logical resource name.</param>
    /// <returns>A texture the caller must dispose.</returns>
    public static Texture2D LoadTexture(GraphicsDevice device, string name)
    {
        using var stream = Open(name);
        return Texture2D.FromStream(device, stream);
    }

    /// <summary>Loads an embedded image with premultiplied alpha, which suits artwork with transparency.</summary>
    /// <param name="device">The graphics device that will own the texture.</param>
    /// <param name="name">The logical resource name.</param>
    /// <returns>A texture the caller must dispose.</returns>
    public static Texture2D LoadPremultipliedTexture(GraphicsDevice device, string name)
    {
        using var stream = Open(name);
        return Texture2D.FromStream(device, stream, DefaultColorProcessors.PremultiplyAlpha);
    }
}
