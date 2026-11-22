using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;

namespace Afterglow.Services;

// When the settings file can't be loaded as a whole (e.g. one value has the wrong type, because it
// was edited by hand, or because a different version of the app stores that setting differently),
// every setting that can still be read is kept, and only the others go back to their defaults
public partial class SettingsService
{
    private static readonly JsonDocumentOptions RecoveryDocumentOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip,
    };

    /// <summary>
    /// Loads each setting from the file on its own, skipping those that can't be read (which keep
    /// their current values, i.e. the defaults after a reset). Within lists (e.g. display
    /// profiles), only the entries that can't be read are skipped. Returns whether any settings
    /// were recovered.
    /// </summary>
    private bool TryRecoverReadableSettings()
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(File.ReadAllText(_filePath), RecoveryDocumentOptions);
        }
        // Not JSON at all (or no longer readable), so there's nothing to recover
        catch (Exception ex)
            when (ex
                    is JsonException
                        or IOException
                        or UnauthorizedAccessException
                        or SecurityException
            )
        {
            Debug.WriteLine($"Failed to read the settings file for recovery: {ex}");
            return false;
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return false;

            var properties = SettingsSerializerContext.Default.SettingsService.Properties;
            var recoveredCount = 0;

            foreach (var jsonProperty in document.RootElement.EnumerateObject())
            {
                // Unknown settings (e.g. from another version) are ignored, as in a regular load
                if (FindProperty(properties, jsonProperty.Name) is not { Set: { } set } property)
                    continue;

                if (!TryDeserialize(jsonProperty.Value, property.PropertyType, out var value))
                {
                    Debug.WriteLine(
                        $"Setting '{jsonProperty.Name}' couldn't be read, using its default."
                    );
                    continue;
                }

                try
                {
                    set(this, value);
                    recoveredCount++;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Setting '{jsonProperty.Name}' couldn't be applied: {ex}");
                }
            }

            return recoveredCount > 0;
        }
    }

    private static JsonPropertyInfo? FindProperty(IList<JsonPropertyInfo> properties, string name)
    {
        foreach (var property in properties)
        {
            if (string.Equals(property.Name, name, StringComparison.Ordinal))
                return property;
        }

        // Also accept names with different casing (e.g. edited by hand)
        foreach (var property in properties)
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                return property;
        }

        return null;
    }

    private static bool TryDeserialize(JsonElement element, Type type, out object? value)
    {
        value = null;

        if (SettingsSerializerContext.Default.GetTypeInfo(type) is not { } typeInfo)
            return false;

        try
        {
            value = element.Deserialize(typeInfo);
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to read a setting value of type {type}: {ex.Message}");
        }

        // A list with some unreadable entries keeps the rest of them
        if (
            element.ValueKind != JsonValueKind.Array
            || TryGetElementType(type) is not { } elementType
        )
            return false;

        var readableItems = new JsonArray();
        foreach (var item in element.EnumerateArray())
        {
            if (TryDeserialize(item, elementType, out _))
                readableItems.Add(JsonNode.Parse(item.GetRawText()));
        }

        try
        {
            value = readableItems.Deserialize(typeInfo);
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to read the remaining entries of type {type}: {ex.Message}");
            return false;
        }
    }

    // The type of the entries of an array or a generic list (e.g. IReadOnlyList<T>)
    private static Type? TryGetElementType(Type type)
    {
        if (type.IsArray)
            return type.GetElementType();

        if (type.IsGenericType && type.GetGenericArguments() is [var argument])
            return argument;

        return null;
    }
}
