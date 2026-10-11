// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Audit stamp of a bulk ExecuteUpdate: such a statement bypasses the context's OnBeforeSaving, so every bulk UPDATE of a
/// schedule entry sets UpdateTime and CurrentUserUpdated itself through this single helper. Readers such as the scenario
/// staleness check rely on UpdateTime to see a lock or owner change.
/// </summary>
/// <param name="At">UTC instant written into UpdateTime</param>
/// <param name="Actor">Audit actor written into CurrentUserUpdated</param>

using Klacks.Api.Domain.Common;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Query;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

internal sealed record BulkUpdateAuditStamp(DateTime At, string Actor)
{
    public static BulkUpdateAuditStamp Now(DataBaseContext context) => Now(context.CurrentAuditActorName());

    public static BulkUpdateAuditStamp Now(string actor) => new(DateTime.UtcNow, actor);

    public UpdateSettersBuilder<TEntity> ApplyTo<TEntity>(UpdateSettersBuilder<TEntity> setters)
        where TEntity : BaseEntity
    {
        var at = At;
        var actor = Actor;
        return setters
            .SetProperty(e => e.UpdateTime, at)
            .SetProperty(e => e.CurrentUserUpdated, actor);
    }
}
