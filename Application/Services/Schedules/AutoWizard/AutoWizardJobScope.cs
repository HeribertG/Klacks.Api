// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Services.Schedules.AutoWizard;

/// <param name="GroupId">Group the job's scenarios belong to; null for a group-less run</param>
/// <param name="AgentIds">Employees the job plans (already limited to those its starter may see)</param>
/// <param name="RecordedAt">When the job started</param>
public sealed record AutoWizardJobScope(Guid? GroupId, IReadOnlyList<Guid> AgentIds, DateTimeOffset RecordedAt);
