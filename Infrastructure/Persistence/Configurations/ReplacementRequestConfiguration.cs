// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// EF Core configuration for the replacement request book. The natural key (scenario token, candidate,
/// source shift, date) is a partial unique index over live rows only, so a soft-deleted proposal never blocks
/// a new one. NULLS NOT DISTINCT (PostgreSQL 15+) makes real-plan rows (token null) collide too, so a second
/// live row for the same real slot and candidate is refused by the database, not only by the writers. The list endpoint filters by absent employee and date, the
/// scenario hooks by token, retention by report time.
/// </summary>

using Klacks.Api.Domain.Models.Schedules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class ReplacementRequestConfiguration : IEntityTypeConfiguration<ReplacementRequest>
{
    private const string LiveRowFilter = "\"is_deleted\" = false";

    public void Configure(EntityTypeBuilder<ReplacementRequest> builder)
    {
        builder.HasQueryFilter(r => !r.IsDeleted);

        builder.HasIndex(r => new { r.AnalyseToken, r.CandidateClientId, r.ShiftId, r.Date })
            .IsUnique()
            .AreNullsDistinct(false)
            .HasFilter(LiveRowFilter);

        builder.HasIndex(r => new { r.AbsentClientId, r.Date });
        builder.HasIndex(r => r.CandidateClientId);
        builder.HasIndex(r => r.ReportedAtUtc);
    }
}
