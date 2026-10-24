// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Turns a captured trajectory into the tuner's view of it. The rerank observation is written into every candidate
/// entry of knowledge_index_candidates_json (TrajectoryCaptureService), so the first entry carries it. A turn is
/// usable only when the user wanted something changed and the turn ran to its end: there the outcome is a fact
/// (the write succeeded and nobody corrected it), while a read-only answer has no reliable success signal.
/// Rows without the observation (older rows, Always/Never passes, turns without candidates) are skipped.
/// </summary>

using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant;

public static class SkillRerankOutcomeReader
{
    /// <summary>The tuner's view of the turn, or null when the turn cannot teach anything about the threshold.</summary>
    public static SkillRerankOutcome? Read(SkillSelectionTrajectory trajectory)
    {
        if (!trajectory.HadMutationIntent || trajectory.WasInterrupted)
        {
            return null;
        }

        var observation = ReadObservation(trajectory.KnowledgeIndexCandidatesJson);
        if (observation == null)
        {
            return null;
        }

        var success = trajectory.WasExecuted && trajectory.WasSuccessful == true && !trajectory.WasCorrected;
        return new SkillRerankOutcome(observation.Value.Margin, observation.Value.Reranked, success);
    }

    private static (double Margin, bool Reranked)? ReadObservation(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array || document.RootElement.GetArrayLength() == 0)
            {
                return null;
            }

            var first = document.RootElement[0];
            if (first.ValueKind != JsonValueKind.Object
                || !first.TryGetProperty(SkillRerankJsonKeys.Margin, out var margin)
                || margin.ValueKind != JsonValueKind.Number
                || !first.TryGetProperty(SkillRerankJsonKeys.Reranked, out var reranked)
                || (reranked.ValueKind != JsonValueKind.True && reranked.ValueKind != JsonValueKind.False))
            {
                return null;
            }

            var value = margin.GetDouble();
            return double.IsFinite(value) ? (value, reranked.GetBoolean()) : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
