// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// EF Core configuration for CounterRule: Enforcement is a nullable per-rule warn/block override stored
/// as an integer (null = the global counterRule enforcement mode applies); ImportSourceKey is unique
/// among ACTIVE IMPORTED rows only — customer-created counter rules all carry the empty string, so the
/// partial index must exclude it, matching the Qualification/Macro/SchedulingRule convention. Origin and
/// ApprovalStatus default to Admin/Approved in the database so every pre-existing row stays effective; both
/// enums leave 0 unused, so the EF default sentinel (0) never swallows an explicitly set value.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
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

        builder.Property(r => r.Origin).HasConversion<int>().HasDefaultValue(RuleOrigin.Admin);
        builder.Property(r => r.ApprovalStatus).HasConversion<int>().HasDefaultValue(RuleApprovalStatus.Approved);
        builder.Property(r => r.SourceText).HasMaxLength(PlanningConstraintDefaults.SourceTextMaxLength);

        builder.Property(r => r.ImportSourceKey).IsRequired().HasMaxLength(ImportSourceKeyMaxLength).HasDefaultValue(string.Empty);
        builder.Property(r => r.ImportContentHash).IsRequired().HasMaxLength(ImportContentHashMaxLength).HasDefaultValue(string.Empty);

        builder.HasIndex(r => r.ImportSourceKey)
            .IsUnique()
            .HasFilter("is_deleted = false AND import_source_key <> ''");
    }
}
