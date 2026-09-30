// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// EF Core configuration for the ClientPeriodHours-Entity: the cache key is unique among live rows only
/// (soft-deleted rows may repeat it), and soft-deleted rows or rows of deleted clients are never read.
/// </summary>
using Klacks.Api.Domain.Models.Schedules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class ClientPeriodHoursConfiguration : IEntityTypeConfiguration<ClientPeriodHours>
{
    private const string LiveRowsFilter = "\"is_deleted\" = false";

    public void Configure(EntityTypeBuilder<ClientPeriodHours> builder)
    {
        builder.HasIndex(p => new { p.ClientId, p.StartDate, p.EndDate, p.AnalyseToken })
            .IsUnique()
            .HasFilter(LiveRowsFilter);

        builder.HasQueryFilter(p => !p.IsDeleted && !p.Client!.IsDeleted);
    }
}
