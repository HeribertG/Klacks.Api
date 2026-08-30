// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Loads turn-selection goldsets from JSON files shipped under Application/Skills/Goldsets/{goldset}.json.
/// </summary>

using System.Text.Json;

namespace Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval;

public class FileTurnGoldsetLoader : ITurnGoldsetLoader
{
    private const string GoldsetSubPath = "Application/Skills/Goldsets";
    private const string ExpectedKindSelection = "turn-selection";
    private const string ExpectedKindHonesty = "turn-honesty";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<IReadOnlyList<TurnGoldsetItem>> LoadAsync(string goldset, CancellationToken cancellationToken = default)
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
        var document = await JsonSerializer.DeserializeAsync<TurnGoldsetDocument>(stream, SerializerOptions, cancellationToken);

        if (document == null
            || (!string.Equals(document.Kind, ExpectedKindSelection, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(document.Kind, ExpectedKindHonesty, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidDataException(
                $"Goldset '{sanitized}' is not a valid {ExpectedKindSelection}/{ExpectedKindHonesty} goldset.");
        }

        return document.Items;
    }
}
