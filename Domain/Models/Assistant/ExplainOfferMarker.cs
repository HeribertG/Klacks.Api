// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The ContentParamsJson of an explain-offer counter row (agent_trigger_dispatches): the explanation depth the offer
/// was made at, the gate it was made at (skill and the fingerprint of the held token, so an acceptance renews exactly
/// that confirmation) and, once settled, the outcome. A row whose JSON cannot be read yields an empty marker, so an
/// unreadable row is settled like any other but never renews a confirmation.
/// </summary>

using System.Text.Json;
using System.Text.Json.Serialization;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Models.Assistant;

public sealed class ExplainOfferMarker
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    [JsonPropertyName(ExplainOfferDefaults.LevelProperty)]
    public string? Level { get; set; }

    [JsonPropertyName(ExplainOfferDefaults.GateSkillProperty)]
    public string? GateSkill { get; set; }

    [JsonPropertyName(ExplainOfferDefaults.TokenFingerprintProperty)]
    public string? TokenFingerprint { get; set; }

    [JsonPropertyName(ExplainOfferDefaults.OutcomeProperty)]
    public string? Outcome { get; set; }

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    /// <summary>The marker stored in a row, an empty one when the JSON is missing or unreadable.</summary>
    /// <param name="json">The row's ContentParamsJson</param>
    public static ExplainOfferMarker Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new ExplainOfferMarker();
        }

        try
        {
            return JsonSerializer.Deserialize<ExplainOfferMarker>(json, JsonOptions) ?? new ExplainOfferMarker();
        }
        catch (JsonException)
        {
            return new ExplainOfferMarker();
        }
    }
}
