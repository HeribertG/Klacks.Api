// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Judges the Holistic Harmonizer pre-flight ping response. Healthy is the expected {"ping":"pong"} JSON,
/// and also a thinking model whose answer was cut off because it spent the output budget on reasoning:
/// such a model is reachable and answering, which is all the ping has to prove; the real proposal call runs
/// with a far larger budget. Provider errors and wrong answers without that signature fail.
/// </summary>

using System.Text.Json;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Infrastructure.Services.Schedules.HolisticHarmonizer;

public static class PlanProposalPingEvaluator
{
    private const string PingProperty = "ping";
    private const string PongValue = "pong";
    private const int ResponsePreviewLength = 120;
    private const string PreviewEllipsis = "...";
    private const string ProviderRejectedError = "Provider rejected the ping.";
    private const string UnexpectedResponseErrorFormat = "Model returned unexpected ping response: {0}";

    public static PlanProposalPingVerdict Evaluate(LLMProviderResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (!response.Success)
        {
            return new PlanProposalPingVerdict(false, response.Error ?? ProviderRejectedError, false);
        }

        var content = response.Content ?? string.Empty;
        if (ContainsPongJson(content))
        {
            return new PlanProposalPingVerdict(true, null, false);
        }

        if (OutputBudgetSpentOnThinking(response))
        {
            return new PlanProposalPingVerdict(true, null, true);
        }

        var preview = content.Length > ResponsePreviewLength ? content[..ResponsePreviewLength] + PreviewEllipsis : content;
        return new PlanProposalPingVerdict(false, string.Format(UnexpectedResponseErrorFormat, preview), false);
    }

    private static bool OutputBudgetSpentOnThinking(LLMProviderResponse response) =>
        response.ReasoningWithoutContent
        || (response.OutputTruncated && response.ReasoningTokens > 0);

    private static bool ContainsPongJson(string content)
    {
        var json = HarmonyJsonParser.ExtractJsonObject(content);
        if (json is null)
        {
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty(PingProperty, out var pingElement)
                && pingElement.ValueKind == JsonValueKind.String
                && string.Equals(pingElement.GetString(), PongValue, StringComparison.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
