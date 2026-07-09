// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Maps raw telemetry candidates into goldset v2 items for human curation: every tool
/// argument becomes an exact-match slot, the source is stamped as telemetry and ids are
/// sequential. The output is a starting point — a human tightens match modes and drops
/// bad turns before the file is committed.
/// </summary>

using System.Text.Json;
using Klacks.Api.Application.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval;

public class TurnGoldsetCandidateExtractor
{
    private const string TelemetrySource = "telemetry";
    private const string CandidateIdPrefix = "tc";

    private static readonly JsonSerializerOptions ParameterOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ITurnGoldsetCandidateRepository _candidateRepository;
    private readonly ILogger<TurnGoldsetCandidateExtractor> _logger;

    public TurnGoldsetCandidateExtractor(
        ITurnGoldsetCandidateRepository candidateRepository,
        ILogger<TurnGoldsetCandidateExtractor> logger)
    {
        _candidateRepository = candidateRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<TurnGoldsetItem>> ExtractAsync(
        DateTime fromDate,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var candidates = await _candidateRepository.GetCandidatesAsync(fromDate, limit, cancellationToken);

        var items = new List<TurnGoldsetItem>(candidates.Count);
        var sequence = 1;

        foreach (var candidate in candidates)
        {
            items.Add(new TurnGoldsetItem
            {
                Id = $"{CandidateIdPrefix}-{sequence++:D3}",
                Message = candidate.Message,
                ExpectedTool = candidate.SkillName,
                ExpectedSlots = ParseSlots(candidate.ParametersJson),
                Source = TelemetrySource,
                Comment = $"Extracted from telemetry ({candidate.Timestamp:yyyy-MM-dd})"
            });
        }

        _logger.LogInformation(
            "TurnGoldsetCandidateExtractor produced {Count} candidates since {FromDate}",
            items.Count, fromDate);

        return items;
    }

    private static List<TurnGoldsetSlot> ParseSlots(string? parametersJson)
    {
        if (string.IsNullOrWhiteSpace(parametersJson))
        {
            return new List<TurnGoldsetSlot>();
        }

        try
        {
            var parameters = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(parametersJson, ParameterOptions);
            if (parameters == null)
            {
                return new List<TurnGoldsetSlot>();
            }

            return parameters
                .Select(p => new TurnGoldsetSlot
                {
                    Name = p.Key,
                    Match = SlotMatchMode.Exact,
                    Value = p.Value.ValueKind == JsonValueKind.String ? p.Value.GetString() : p.Value.GetRawText()
                })
                .ToList();
        }
        catch (JsonException)
        {
            return new List<TurnGoldsetSlot>();
        }
    }
}
