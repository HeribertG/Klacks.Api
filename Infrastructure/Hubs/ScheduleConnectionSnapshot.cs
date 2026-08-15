// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Infrastructure.Hubs;

/// <summary>
/// Complete state of one registered schedule connection, returned by a single tracker query.
/// </summary>
/// <param name="ConnectionId">SignalR connection id the state belongs to</param>
/// <param name="Start">First day of the date range the connection observes</param>
/// <param name="End">Last day of the date range the connection observes</param>
/// <param name="AnalyseToken">Scenario partition key; null means the original schedule</param>
/// <param name="SelectedGroupId">Group filter of the connection; null means no group filter</param>
public sealed record ScheduleConnectionSnapshot(
    string ConnectionId,
    DateOnly Start,
    DateOnly End,
    Guid? AnalyseToken,
    Guid? SelectedGroupId);
