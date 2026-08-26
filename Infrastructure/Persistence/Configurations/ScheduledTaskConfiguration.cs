// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the ScheduledTask entity: soft-delete query filter, a per-owner unique
/// name index over non-deleted rows, and a due-scan index over enabled, unpaused tasks by next run.
/// The two flags deliberately get no HasDefaultValue: their CLR initializer is already false, and a
/// store default would make EF treat false as an unset sentinel (warning 20601).
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

        builder.HasIndex(p => new { p.IsEnabled, p.IsPaused, p.NextRunUtc });
    }
}
