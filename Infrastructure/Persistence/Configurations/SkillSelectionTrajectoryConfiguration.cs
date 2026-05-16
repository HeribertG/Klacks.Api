// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for SkillSelectionTrajectory with table name, query filter and indexes.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class SkillSelectionTrajectoryConfiguration : IEntityTypeConfiguration<SkillSelectionTrajectory>
{
    public void Configure(EntityTypeBuilder<SkillSelectionTrajectory> builder)
    {
        builder.ToTable("skill_selection_trajectories");
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => new { p.AgentId, p.CreateTime });
        builder.HasIndex(p => p.WasCorrected);
        builder.HasIndex(p => p.PlanId);
        builder.Property(p => p.Locale).HasMaxLength(8);
        builder.Property(p => p.UserMessageHash).HasMaxLength(16);
        builder.Property(p => p.IntentExcerpt).HasMaxLength(120);
        builder.Property(p => p.LlmChosenSkill).HasMaxLength(128);
        builder.Property(p => p.CorrectionType).HasMaxLength(32);
        builder.Property(p => p.KnowledgeIndexCandidatesJson).HasColumnType("jsonb");

        builder.HasOne<AgentPlan>()
            .WithMany()
            .HasForeignKey(p => p.PlanId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
