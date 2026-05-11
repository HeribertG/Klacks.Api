// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Loads goldsets from JSON files shipped under Application/Skills/Goldsets/{goldset}.json.
/// </summary>

using System.Text.Json;

namespace Klacks.Api.Application.Services.Assistant.Evaluation;

public class FileGoldsetLoader : IGoldsetLoader
{
    private const string GoldsetSubPath = "Application/Skills/Goldsets";

    public async Task<IReadOnlyList<GoldsetItem>> LoadAsync(string goldset, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(goldset))
        {
            throw new ArgumentException("Goldset name must be provided.", nameof(goldset));
        }

        var sanitized = goldset.Trim().Replace("/", string.Empty).Replace("\\", string.Empty);
        var path = Path.Combine(AppContext.BaseDirectory, GoldsetSubPath, $"{sanitized}.json");

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Goldset '{sanitized}' not found at {path}.");
        }

        await using var stream = File.OpenRead(path);
        var raw = await JsonSerializer.DeserializeAsync<List<GoldsetItem>>(stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
            cancellationToken);

        return raw ?? new List<GoldsetItem>();
    }
}
