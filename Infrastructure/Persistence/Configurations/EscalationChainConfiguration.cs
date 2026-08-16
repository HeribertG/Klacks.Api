// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the EscalationChain entity. The partial unique index enforces at most
/// one Running chain per shift: CoverAbsenceSkill can be invoked more than once for the same slot,
/// and without this guard a second call would start a second, independent chain that wakes the same
/// roster twice for the same outage.
/// </summary>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant.Escalation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class EscalationChainConfiguration : IEntityTypeConfiguration<EscalationChain>
{
    public void Configure(EntityTypeBuilder<EscalationChain> builder)
    {
        builder.HasQueryFilter(c => !c.IsDeleted);

        builder.HasIndex(c => c.Status);
        builder.HasIndex(c => c.WorkId);
        builder.HasIndex(c => c.AbsenceBreakId);

        builder.HasIndex(c => c.WorkId)
            .IsUnique()
            .HasFilter($"\"is_deleted\" = false AND \"status\" = {(int)EscalationChainStatus.Running}");

        builder.HasMany(c => c.Stages)
            .WithOne(s => s.Chain)
            .HasForeignKey(s => s.EscalationChainId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
