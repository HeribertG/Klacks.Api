// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reads and writes the evidence of goldset-born proposals. Older rows hold a bare array of messages and
/// correction-born or recipe rows other shapes; Parse returns evidence without items for all of them, and for
/// empty or malformed JSON.
/// </summary>
using System.Text.Json;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant;

public static class GoldsetMissEvidenceCodec
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public static string Serialize(GoldsetMissEvidence evidence) => JsonSerializer.Serialize(evidence, Options);

    public static GoldsetMissEvidence Parse(string? evidenceJson)
    {
        if (string.IsNullOrWhiteSpace(evidenceJson))
        {
            return GoldsetMissEvidence.Empty;
        }

        try
        {
            using var document = JsonDocument.Parse(evidenceJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return GoldsetMissEvidence.Empty;
            }

            var parsed = document.RootElement.Deserialize<GoldsetMissEvidence>(Options);
            return parsed == null
                ? GoldsetMissEvidence.Empty
                : new GoldsetMissEvidence(
                    parsed.Examples ?? Array.Empty<string>(),
                    (parsed.Items ?? Array.Empty<GoldsetItemRef>())
                        .Where(item => item != null
                            && !string.IsNullOrWhiteSpace(item.Goldset)
                            && !string.IsNullOrWhiteSpace(item.ItemId))
                        .ToList());
        }
        catch (JsonException)
        {
            return GoldsetMissEvidence.Empty;
        }
    }
}
