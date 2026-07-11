// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Loads speech-wer goldsets from JSON files shipped under Application/Skills/Goldsets/{goldset}.json
/// and resolves the audio files referenced by goldset items to absolute paths inside that folder.
/// </summary>
/// <param name="goldset">Goldset file name without extension (e.g. "speech-wer-v1")</param>
/// <param name="audioFile">Audio path of a goldset item, relative to the goldset folder</param>

using System.Text.Json;

namespace Klacks.Api.Application.Services.Assistant.Evaluation.SpeechEval;

public class FileSpeechGoldsetLoader : ISpeechGoldsetLoader
{
    private const string GoldsetSubPath = "Application/Skills/Goldsets";
    private const string ExpectedKind = "speech-wer";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<IReadOnlyList<SpeechGoldsetItem>> LoadAsync(string goldset, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(goldset))
        {
            throw new ArgumentException("Goldset name must be provided.", nameof(goldset));
        }

        var sanitized = goldset.Trim().Replace("/", string.Empty).Replace("\\", string.Empty);
        var path = Path.Combine(GoldsetDirectory, $"{sanitized}.json");

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Goldset '{sanitized}' not found at {path}.");
        }

        await using var stream = File.OpenRead(path);
        var document = await JsonSerializer.DeserializeAsync<SpeechGoldsetDocument>(stream, SerializerOptions, cancellationToken);

        if (document == null || !string.Equals(document.Kind, ExpectedKind, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"Goldset '{sanitized}' is not a valid {ExpectedKind} goldset.");
        }

        return document.Items;
    }

    public string ResolveAudioPath(string audioFile)
    {
        if (string.IsNullOrWhiteSpace(audioFile))
        {
            throw new ArgumentException("Audio file path must be provided.", nameof(audioFile));
        }

        var goldsetDirectory = Path.GetFullPath(GoldsetDirectory);
        var resolved = Path.GetFullPath(Path.Combine(goldsetDirectory, audioFile));

        if (!resolved.StartsWith(goldsetDirectory + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Audio file path '{audioFile}' escapes the goldset folder.");
        }

        return resolved;
    }

    private static string GoldsetDirectory => Path.Combine(AppContext.BaseDirectory, GoldsetSubPath);
}
