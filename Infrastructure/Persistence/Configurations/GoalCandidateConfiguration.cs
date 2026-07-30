// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the GoalCandidate entity (self-directed goals, Phase 1 shadow mode).
/// </summary>

using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class GoalCandidateConfiguration : IEntityTypeConfiguration<GoalCandidate>
{
    public void Configure(EntityTypeBuilder<GoalCandidate> builder)
    {
        builder.HasQueryFilter(c => !c.IsDeleted);

        builder.Property(c => c.UserId).HasMaxLength(64);
        builder.Property(c => c.GoalType).HasMaxLength(64);
        builder.Property(c => c.RationaleParamsJson).HasMaxLength(512);
        builder.Property(c => c.Title).IsRequired().HasMaxLength(256);
        builder.Property(c => c.Rationale).IsRequired().HasMaxLength(2048);
        builder.Property(c => c.Status).IsRequired().HasMaxLength(32);
        builder.Property(c => c.Confidence).IsRequired().HasMaxLength(16);
        builder.Property(c => c.SignalSource).IsRequired().HasMaxLength(128);
        builder.Property(c => c.DedupHash).IsRequired().HasMaxLength(128);

        builder.HasIndex(c => new { c.UserId, c.DedupHash });
        builder.HasIndex(c => c.Status).HasFilter("status IN ('shadow','proposed')");
        builder.HasIndex(c => c.PlanId).HasFilter("plan_id IS NOT NULL");
    }
}
