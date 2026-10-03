// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Cheap, cached answer to "is there any approved planning constraint at all", so write gates and live checks
/// of an installation without planning constraints do not query the constraint table on every call. The handlers
/// that change the approved set (create, update, approve, revoke) invalidate it.
/// </summary>

namespace Klacks.Api.Application.Interfaces.Schedules;

public interface IPlanningConstraintPresence
{
    Task<bool> AnyApprovedAsync(CancellationToken cancellationToken = default);

    void Invalidate();
}
