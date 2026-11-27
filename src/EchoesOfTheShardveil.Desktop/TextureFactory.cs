// 261127_code
// 261127_documentation

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EchoesOfTheShardveil.Desktop;

/// <summary>Creates textures from pixels computed in code.</summary>
internal static class TextureFactory
{
    /// <summary>Uploads a row-major pixel array to a new texture.</summary>
    /// <param name="device">The graphics device that will own the texture.</param>
    /// <param name="width">The texture width in pixels.</param>
    /// <param name="height">The texture height in pixels.</param>
    /// <param name="pixels">Exactly <paramref name="width"/> times <paramref name="height"/> colors.</param>
    /// <returns>A texture the caller must dispose.</returns>
    public static Texture2D FromPixels(GraphicsDevice device, int width, int height, Color[] pixels)
    {
        var texture = new Texture2D(device, width, height);
        texture.SetData(pixels);
        return texture;
    }
}
