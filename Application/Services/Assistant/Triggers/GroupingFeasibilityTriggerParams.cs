// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Shared summary parameters, payload and severity of both grouping feasibility events, keyed after the
/// placeholders of assistant.proactive.groupingFeasibility so ProactiveContentParamMerge renders the
/// current payload.
/// </summary>
/// <param name="counts">Numbers of the report shown in the inbox sentence.</param>

using System.Globalization;
using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public static class GroupingFeasibilityTriggerParams
{
    public const string Shifts = "shifts";
    public const string Clients = "clients";
    public const string Capacity = "capacity";
    public const string Proposals = "proposals";

    public static IReadOnlyDictionary<string, string> Summary(GroupingFeasibilityCounts counts) => new Dictionary<string, string>
    {
        [Shifts] = counts.UnfillableShifts.ToString(CultureInfo.InvariantCulture),
        [Clients] = counts.UnmatchedClients.ToString(CultureInfo.InvariantCulture),
        [Capacity] = counts.CapacityShortfalls.ToString(CultureInfo.InvariantCulture),
        [Proposals] = counts.Proposals.ToString(CultureInfo.InvariantCulture),
    };

    public static IReadOnlyDictionary<string, object?> Payload(GroupingFeasibilityCounts counts) => new Dictionary<string, object?>
    {
        [Shifts] = counts.UnfillableShifts,
        [Clients] = counts.UnmatchedClients,
        [Capacity] = counts.CapacityShortfalls,
        [Proposals] = counts.Proposals,
    };

    public static string SeverityFor(GroupingFeasibilityCounts counts) =>
        counts.UnfillableShifts > 0 ? AgentTriggerSeverity.Medium : AgentTriggerSeverity.Low;
}
