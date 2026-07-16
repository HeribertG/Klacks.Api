// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for CounterRule: Enforcement is a nullable per-rule warn/block override stored
/// as an integer (null = the global counterRule enforcement mode applies); ImportSourceKey is unique
/// among ACTIVE IMPORTED rows only — customer-created counter rules all carry the empty string, so the
/// partial index must exclude it, matching the Qualification/Macro/SchedulingRule convention.
/// </summary>

using Klacks.Api.Domain.Models.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class CounterRuleConfiguration : IEntityTypeConfiguration<CounterRule>
{
    private const int ImportSourceKeyMaxLength = 200;
    private const int ImportContentHashMaxLength = 64;

    public void Configure(EntityTypeBuilder<CounterRule> builder)
    {
        builder.Property(r => r.Enforcement).HasConversion<int?>();

        builder.Property(r => r.ImportSourceKey).IsRequired().HasMaxLength(ImportSourceKeyMaxLength).HasDefaultValue(string.Empty);
        builder.Property(r => r.ImportContentHash).IsRequired().HasMaxLength(ImportContentHashMaxLength).HasDefaultValue(string.Empty);

        builder.HasIndex(r => r.ImportSourceKey)
            .IsUnique()
            .HasFilter("is_deleted = false AND import_source_key <> ''");
    }
}
