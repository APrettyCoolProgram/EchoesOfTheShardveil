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

    /// <summary>Resolves an AppData-relative content reference from a configuration file.</summary>
    /// <param name="reference">A relative path using forward slashes.</param>
    /// <returns>The absolute path inside AppData.</returns>
    public static string ResolveReference(string reference)
    {
        var parts = reference.Split('/');
        if (parts.Any(part => string.IsNullOrWhiteSpace(part) || part is "." or ".." ||
            part.Contains('\\') || part.Contains(':')))
        {
            throw new InvalidDataException($"Invalid AppData reference '{reference}'.");
        }
        return PathFor(parts);
    }

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

    /// <summary>Reads one object from a larger AppData document.</summary>
    /// <typeparam name="T">The object type.</typeparam>
    /// <param name="element">The JSON element to read.</param>
    /// <param name="path">The containing file path for diagnostics.</param>
    /// <returns>The deserialized object.</returns>
    public static T ReadElement<T>(JsonElement element, string path) where T : class
    {
        try
        {
            return element.Deserialize<T>(Options)
                ?? throw new InvalidDataException($"'{path}' contains an empty object.");
        }
        catch (JsonException error)
        {
            throw new InvalidDataException($"Invalid JSON object in '{path}': {error.Message}", error);
        }
    }
}
