// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Loads turn-intent-head.v1.json from the build output once, on first use. A missing, unreadable or inconsistent
/// file is logged and yields no head, which switches the classifier off instead of failing a chat turn.
/// </summary>
/// <param name="logger">Receives the reason a head could not be loaded</param>

using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant.TurnIntent;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public sealed class FileTurnIntentHeadProvider : ITurnIntentHeadProvider
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly Lazy<TurnIntentHeadWeights?> _head;
    private readonly ILogger<FileTurnIntentHeadProvider> _logger;

    public FileTurnIntentHeadProvider(ILogger<FileTurnIntentHeadProvider> logger)
        : this(Path.Combine(AppContext.BaseDirectory, TurnIntentDefaults.HeadFileRelativePath), logger)
    {
    }

    public FileTurnIntentHeadProvider(string path, ILogger<FileTurnIntentHeadProvider> logger)
    {
        _logger = logger;
        _head = new Lazy<TurnIntentHeadWeights?>(() => Load(path), LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public TurnIntentHeadWeights? Current => _head.Value;

    private TurnIntentHeadWeights? Load(string path)
    {
        if (!File.Exists(path))
        {
            _logger.LogInformation("{Prefix} no head file at {Path}; classifier stays inactive", TurnIntentDefaults.LogPrefix, path);
            return null;
        }

        try
        {
            var head = JsonSerializer.Deserialize<TurnIntentHeadWeights>(File.ReadAllText(path), SerializerOptions);
            var problem = head == null ? "empty file" : Validate(head);
            if (problem != null)
            {
                _logger.LogError("{Prefix} head file {Path} rejected: {Problem}", TurnIntentDefaults.LogPrefix, path, problem);
                return null;
            }

            return head;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            _logger.LogError(ex, "{Prefix} head file {Path} could not be read", TurnIntentDefaults.LogPrefix, path);
            return null;
        }
    }

    public static string? Validate(TurnIntentHeadWeights head)
    {
        var expectedClasses = Enum.GetNames<TurnIntentLabel>();

        if (head.Classes is null || head.HandFeatureNames is null || head.Means is null || head.Scales is null
            || head.Weights is null || head.Bias is null || head.Locales is null
            || head.Weights.Any(row => row is null) || head.Locales.Any(locale => locale is null))
        {
            return "missing arrays";
        }

        var columns = head.Means.Length;

        if (head.Version != TurnIntentDefaults.HeadFormatVersion)
        {
            return $"format version {head.Version}, expected {TurnIntentDefaults.HeadFormatVersion}";
        }

        if (!head.Classes.SequenceEqual(expectedClasses, StringComparer.Ordinal))
        {
            return $"classes [{string.Join(",", head.Classes)}], expected [{string.Join(",", expectedClasses)}]";
        }

        if (!head.HandFeatureNames.SequenceEqual(TurnIntentHandFeatures.Names, StringComparer.Ordinal))
        {
            return "hand-feature columns differ from TurnIntentHandFeatures.Names";
        }

        if (columns <= head.HandFeatureNames.Length || head.Scales.Length != columns)
        {
            return "standardiser size mismatch";
        }

        if (head.Weights.Length != expectedClasses.Length || head.Bias.Length != expectedClasses.Length
            || head.Weights.Any(row => row.Length != columns))
        {
            return "weight matrix size mismatch";
        }

        if (head.Temperature <= 0.0)
        {
            return "temperature must be positive";
        }

        return null;
    }
}
