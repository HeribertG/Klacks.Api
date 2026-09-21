// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the EscalationChain entity. Two partial unique indexes, one per purpose
/// key, enforce at most one Running chain per shift and at most one per condition: CoverAbsenceSkill
/// can be invoked more than once for the same slot, and the proactive tick re-observes the same open
/// condition every run - without these guards a second call would start a second, independent chain
/// that wakes the same roster twice for the same case. Each filter is restricted to rows that carry
/// its key, so an AbsenceCoverage chain (ConditionId null) never collides with an approval chain
/// (WorkId null) and vice versa.
/// </summary>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant.Escalation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class EscalationChainConfiguration : IEntityTypeConfiguration<EscalationChain>
{
    private const string RunningRowFilter = "\"is_deleted\" = false AND \"status\" = ";

    public void Configure(EntityTypeBuilder<EscalationChain> builder)
    {
        builder.HasQueryFilter(c => !c.IsDeleted);

        builder.HasIndex(c => c.Status);
        builder.HasIndex(c => c.AbsenceBreakId);

        builder.HasIndex(c => c.WorkId)
            .IsUnique()
            .HasFilter($"\"work_id\" IS NOT NULL AND {RunningRowFilter}{(int)EscalationChainStatus.Running}");

        builder.HasIndex(c => c.ConditionId)
            .IsUnique()
            .HasFilter($"\"condition_id\" IS NOT NULL AND {RunningRowFilter}{(int)EscalationChainStatus.Running}");

        builder.HasMany(c => c.Stages)
            .WithOne(s => s.Chain)
            .HasForeignKey(s => s.EscalationChainId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
