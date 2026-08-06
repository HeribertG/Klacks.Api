// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for HolidayWorkExemptionRule, following the PeriodCapRule convention:
/// ImportSourceKey is unique among ACTIVE IMPORTED rows only, because customer-created exemptions all
/// carry the empty string and would otherwise collide with each other and with soft-deleted
/// predecessors.
/// </summary>

using Klacks.Api.Domain.Models.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class HolidayWorkExemptionRuleConfiguration : IEntityTypeConfiguration<HolidayWorkExemptionRule>
{
    private const int DescriptionMaxLength = 400;
    private const int ImportSourceKeyMaxLength = 200;
    private const int ImportContentHashMaxLength = 64;

    public void Configure(EntityTypeBuilder<HolidayWorkExemptionRule> builder)
    {
        builder.Property(r => r.Description).IsRequired().HasMaxLength(DescriptionMaxLength).HasDefaultValue(string.Empty);
        builder.Property(r => r.ImportSourceKey).IsRequired().HasMaxLength(ImportSourceKeyMaxLength).HasDefaultValue(string.Empty);
        builder.Property(r => r.ImportContentHash).IsRequired().HasMaxLength(ImportContentHashMaxLength);

        builder.HasIndex(r => r.ImportSourceKey)
            .IsUnique()
            .HasFilter("is_deleted = false AND import_source_key <> ''");
    }
}
