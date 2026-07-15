// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the SchedulingRule-Entity with query filter and Index. ImportSourceKey is
/// unique among ACTIVE IMPORTED rows only — customer-created rules all carry the empty string, so the
/// partial index must exclude it (unlike PeriodCapRule, whose rows are exclusively import-created).
/// </summary>
using Klacks.Api.Domain.Models.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class SchedulingRuleConfiguration : IEntityTypeConfiguration<SchedulingRule>
{
    private const int ImportSourceKeyMaxLength = 200;
    private const int ImportContentHashMaxLength = 64;

    public void Configure(EntityTypeBuilder<SchedulingRule> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => new { p.IsDeleted, p.Name });

        builder.Property(p => p.ImportSourceKey).IsRequired().HasMaxLength(ImportSourceKeyMaxLength).HasDefaultValue(string.Empty);
        builder.Property(p => p.ImportContentHash).IsRequired().HasMaxLength(ImportContentHashMaxLength).HasDefaultValue(string.Empty);

        builder.HasIndex(p => p.ImportSourceKey)
            .IsUnique()
            .HasFilter("is_deleted = false AND import_source_key <> ''");
    }
}
