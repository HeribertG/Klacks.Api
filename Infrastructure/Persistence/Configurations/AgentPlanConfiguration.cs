// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the AgentPlan entity (Phase 2 of the autonomy roadmap).
/// </summary>

using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class AgentPlanConfiguration : IEntityTypeConfiguration<AgentPlan>
{
    public void Configure(EntityTypeBuilder<AgentPlan> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.Property(p => p.Goal).IsRequired().HasMaxLength(2048);
        builder.Property(p => p.Status).IsRequired().HasMaxLength(32);
        builder.Property(p => p.StepsJson).HasColumnType("jsonb");
        builder.Property(p => p.LastErrorMessage).HasMaxLength(2048);
        builder.Property(p => p.UserId).HasMaxLength(64);

        builder.HasIndex(p => new { p.AgentId, p.Status });
        builder.HasIndex(p => p.UserId).HasFilter("user_id IS NOT NULL");
        builder.HasIndex(p => p.SessionId).HasFilter("session_id IS NOT NULL");
        builder.HasIndex(p => p.Status).HasFilter("status IN ('drafting','executing','paused_for_approval')");
    }
}
