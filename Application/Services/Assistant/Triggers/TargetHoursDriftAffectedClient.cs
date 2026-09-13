// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// One employee inside an aggregated target-hours-drift finding: who they are and by how much their
/// accumulated hours miss their guaranteed hours in the scanned period.
/// </summary>
namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record TargetHoursDriftAffectedClient(Guid ClientId, string ClientName, decimal DriftHours);
