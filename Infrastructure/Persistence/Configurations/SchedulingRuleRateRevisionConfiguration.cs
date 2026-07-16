// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for SchedulingRuleRateRevision: a soft-delete query filter, a partial unique
/// index on (SchedulingRuleId, ValidFrom) among active rows so a revision date is unique per rule, and a
/// partial unique index on ImportSourceKey among active imported rows (mirroring SchedulingRule, whose
/// natural keys the K20 reconciliation looks up). The parent FK uses Restrict so a rule cannot be removed
/// while dated revisions still reference it.
/// </summary>

using Klacks.Api.Domain.Models.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class SchedulingRuleRateRevisionConfiguration : IEntityTypeConfiguration<SchedulingRuleRateRevision>
{
    private const int ImportSourceKeyMaxLength = 200;
    private const int ImportContentHashMaxLength = 64;

    public void Configure(EntityTypeBuilder<SchedulingRuleRateRevision> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.Property(p => p.ImportSourceKey).IsRequired().HasMaxLength(ImportSourceKeyMaxLength).HasDefaultValue(string.Empty);
        builder.Property(p => p.ImportContentHash).IsRequired().HasMaxLength(ImportContentHashMaxLength).HasDefaultValue(string.Empty);

        builder.HasOne(p => p.SchedulingRule)
            .WithMany(r => r.RateRevisions)
            .HasForeignKey(p => p.SchedulingRuleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => new { p.SchedulingRuleId, p.ValidFrom })
            .IsUnique()
            .HasFilter("is_deleted = false");

        builder.HasIndex(p => p.ImportSourceKey)
            .IsUnique()
            .HasFilter("is_deleted = false AND import_source_key <> ''");
    }
}
