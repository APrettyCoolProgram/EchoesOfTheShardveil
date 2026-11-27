// 261127_code
// 261127_documentation

using System.Text.Json;
using System.Text.Json.Serialization;

namespace EchoesOfTheShardveil.Desktop;

/// <summary>Reads JSON files distributed with the desktop app.</summary>
internal static class AppDataJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        RespectRequiredConstructorParameters = true,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) },
    };

    /// <summary>Resolves a file in the deployed AppData directory.</summary>
    /// <param name="parts">The path segments relative to AppData.</param>
    /// <returns>The absolute path of the data file.</returns>
    public static string PathFor(params string[] parts) =>
        Path.Combine([AppContext.BaseDirectory, "AppData", .. parts]);

    /// <summary>Reads a JSON document, reporting its file path if parsing fails.</summary>
    /// <typeparam name="T">The document type.</typeparam>
    /// <param name="path">The path of the data file.</param>
    /// <returns>The deserialized document.</returns>
    public static T Load<T>(string path) where T : class
    {
        using var stream = File.OpenRead(path);
        try
        {
            return JsonSerializer.Deserialize<T>(stream, Options)
                ?? throw new InvalidDataException($"'{path}' must contain a JSON document.");
        }
        catch (JsonException error)
        {
            throw new InvalidDataException($"Invalid JSON in '{path}': {error.Message}", error);
        }
    }
}
