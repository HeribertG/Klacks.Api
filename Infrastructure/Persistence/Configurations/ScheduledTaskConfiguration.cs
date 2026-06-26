// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the ScheduledTask entity: soft-delete query filter, a per-owner unique
/// name index over non-deleted rows, and a due-scan index over enabled tasks by next run.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class ScheduledTaskConfiguration : IEntityTypeConfiguration<ScheduledTask>
{
    public void Configure(EntityTypeBuilder<ScheduledTask> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.HasIndex(p => new { p.OwnerUserId, p.Name })
            .HasFilter("is_deleted = false")
            .IsUnique();

        builder.HasIndex(p => new { p.IsEnabled, p.NextRunUtc });
    }
}
