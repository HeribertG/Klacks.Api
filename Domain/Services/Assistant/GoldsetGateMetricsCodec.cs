// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Writes gate metrics as camelCase JSON for proposed_skill_changes.gate_metrics_json; the export script reads
/// the same property names. Reads back how many earlier gate runs could not measure a proposal: zero for no,
/// malformed or measured metrics, the stored count for a not_measured verdict (one when the count is missing).
/// </summary>
using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant;

public static class GoldsetGateMetricsCodec
{
    private const string VerdictProperty = "verdict";
    private const string UnmeasuredAttemptsProperty = "unmeasuredAttempts";
    private const int ImpliedUnmeasuredAttempts = 1;

    private static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static string Serialize(GoldsetGateMetrics metrics) => JsonSerializer.Serialize(metrics, Options);

    public static int ReadUnmeasuredAttempts(string? metricsJson)
    {
        if (string.IsNullOrWhiteSpace(metricsJson))
        {
            return 0;
        }

        try
        {
            using var document = JsonDocument.Parse(metricsJson);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty(VerdictProperty, out var verdict)
                || verdict.ValueKind != JsonValueKind.String
                || !string.Equals(verdict.GetString(), GoldsetGateVerdicts.NotMeasured, StringComparison.Ordinal))
            {
                return 0;
            }

            return root.TryGetProperty(UnmeasuredAttemptsProperty, out var attempts)
                && attempts.ValueKind == JsonValueKind.Number
                && attempts.TryGetInt32(out var count)
                && count > 0
                    ? count
                    : ImpliedUnmeasuredAttempts;
        }
        catch (JsonException)
        {
            return 0;
        }
    }
}
