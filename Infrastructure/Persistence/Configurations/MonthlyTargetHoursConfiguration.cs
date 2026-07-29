// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the MonthlyTargetHours-Entity with query filter and a partial unique
/// index on year and month that only covers active rows, so a soft-deleted row never blocks a new
/// one for the same month.
/// </summary>
using Klacks.Api.Domain.Models.Schedules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class MonthlyTargetHoursConfiguration : IEntityTypeConfiguration<MonthlyTargetHours>
{
    public void Configure(EntityTypeBuilder<MonthlyTargetHours> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.HasIndex(p => new { p.Year, p.Month })
            .IsUnique()
            .HasFilter("\"is_deleted\" = false");
    }
}
