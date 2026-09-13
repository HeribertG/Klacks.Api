// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Fired when the turn-selection eval lost ground between two consecutive full runs of the same model and
/// scoring rules. An administrator concern, not a scheduling gap, so it reaches admins only. Severity is
/// medium deliberately: it must show up as an inbox line and a badge and must never interrupt a
/// conversation - the release it concerns has already happened, and there is nothing to do this minute.
/// There is no "warning" severity in this pipeline; medium is the level the weekly learning digest uses
/// for exactly the same reason.
/// </summary>
/// <param name="RunId">The newer of the two compared runs; also the dedup key, so one alert per run</param>
/// <param name="Goldset">The goldset both runs measured</param>
/// <param name="Model">The model both runs were replayed against</param>
/// <param name="Retrieval">RetrievalHit of the newer run</param>
/// <param name="Selection">SelectionHit of the newer run</param>
/// <param name="RetrievalDrop">How far RetrievalHit fell, zero when it did not fall</param>
/// <param name="SelectionDrop">How far SelectionHit fell, zero when it did not fall</param>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record EvalRegressionTriggerEvent(
    Guid RunId,
    string Goldset,
    string Model,
    double Retrieval,
    double Selection,
    double RetrievalDrop,
    double SelectionDrop) : IAgentTriggerEvent
{
    private const string NumberFormat = "F2";

    public string Kind => AgentTriggerKinds.EvalRegression;

    public string Severity => AgentTriggerSeverity.Medium;

    public bool AdminOnly => true;

    public string Summary => ProactiveMessageMarkers.I18nPrefix + ProactiveMessageI18nKeys.EvalRegression;

    public IReadOnlyDictionary<string, string> SummaryParams => new Dictionary<string, string>
    {
        ["goldset"] = Goldset,
        ["model"] = Model,
        ["retrieval"] = Retrieval.ToString(NumberFormat, CultureInfo.InvariantCulture),
        ["selection"] = Selection.ToString(NumberFormat, CultureInfo.InvariantCulture),
        ["retrievalDelta"] = RetrievalDrop.ToString(NumberFormat, CultureInfo.InvariantCulture),
        ["selectionDelta"] = SelectionDrop.ToString(NumberFormat, CultureInfo.InvariantCulture)
    };

    public string DedupKey => RunId.ToString();

    public string? ActionRoute => ProactiveActionRoutes.Settings;

    public IReadOnlyDictionary<string, string>? ActionParams => new Dictionary<string, string>
    {
        [ProactiveActionParamKeys.Target] = ProactiveActionRoutes.SettingsTargetKlacksyLearning
    };

    public IReadOnlyDictionary<string, object?> Payload => new Dictionary<string, object?>
    {
        ["runId"] = RunId,
        ["goldset"] = Goldset,
        ["model"] = Model,
        ["retrievalHit"] = Retrieval,
        ["selectionHit"] = Selection,
        ["retrievalDrop"] = RetrievalDrop,
        ["selectionDrop"] = SelectionDrop
    };
}
