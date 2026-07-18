// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for PeriodCapRule: ImportSourceKey is unique among ACTIVE IMPORTED rows only —
/// customer-created period cap rules all carry the empty string, so the partial index must exclude it,
/// matching the CounterRule/RestrictedTimeWindowRule convention. The K20 entity-import reconciliation can
/// still look an imported row up by its natural key without colliding with a soft-deleted predecessor.
/// </summary>

using Klacks.Api.Domain.Models.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class PeriodCapRuleConfiguration : IEntityTypeConfiguration<PeriodCapRule>
{
    private const int ImportSourceKeyMaxLength = 200;
    private const int ImportContentHashMaxLength = 64;

    public void Configure(EntityTypeBuilder<PeriodCapRule> builder)
    {
        builder.Property(r => r.ImportSourceKey).IsRequired().HasMaxLength(ImportSourceKeyMaxLength).HasDefaultValue(string.Empty);
        builder.Property(r => r.ImportContentHash).IsRequired().HasMaxLength(ImportContentHashMaxLength);

        builder.HasIndex(r => r.ImportSourceKey)
            .IsUnique()
            .HasFilter("is_deleted = false AND import_source_key <> ''");
    }
}
