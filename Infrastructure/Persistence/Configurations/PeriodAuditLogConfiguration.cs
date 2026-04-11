// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the PeriodAuditLog entity with query filter and indexes.
/// </summary>
using Klacks.Api.Domain.Models.Schedules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class PeriodAuditLogConfiguration : IEntityTypeConfiguration<PeriodAuditLog>
{
    public void Configure(EntityTypeBuilder<PeriodAuditLog> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => new { p.StartDate, p.EndDate, p.GroupId });
        builder.HasIndex(p => p.PerformedAt);
    }
}
