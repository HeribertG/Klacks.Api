// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Statische Hilfsklasse zum Laden von Plugin-Datendateien aus dem Dateisystem.
/// </summary>
/// <param name="filePath">Vollständiger Pfad zur JSON-Datei</param>

using System.Text.Json;

namespace Klacks.Api.Infrastructure.Services.Settings;

public static class LanguagePluginFileLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static List<T>? LoadPluginDataFile<T>(string pluginDirectory, string code, string fileName, ILogger logger)
    {
        var filePath = Path.Combine(pluginDirectory, code, fileName);
        if (!File.Exists(filePath))
            return null;

        try
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<List<T>>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load plugin data file '{FileName}' for language '{Code}'", fileName, code);
            return null;
        }
    }
}
