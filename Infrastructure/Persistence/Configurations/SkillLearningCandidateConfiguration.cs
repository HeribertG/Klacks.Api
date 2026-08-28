// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for SkillLearningCandidate.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class SkillLearningCandidateConfiguration : IEntityTypeConfiguration<SkillLearningCandidate>
{
    private const int KindMaxLength = 16;
    private const int StatusMaxLength = 24;

    public void Configure(EntityTypeBuilder<SkillLearningCandidate> builder)
    {
        builder.ToTable("skill_learning_candidates");
        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.Property(p => p.Kind).HasMaxLength(KindMaxLength).IsRequired();
        builder.Property(p => p.Status).HasMaxLength(StatusMaxLength).IsRequired();
        builder.Property(p => p.PayloadJson).HasColumnType("jsonb");
        builder.Property(p => p.RoutingResultJson).HasColumnType("jsonb");
        builder.Property(p => p.ExecutionResultJson).HasColumnType("jsonb");

        builder.HasIndex(p => new { p.ClusterId, p.VariantNo });
        builder.HasIndex(p => p.Status);

        builder.HasOne<SkillLearningCluster>()
            .WithMany()
            .HasForeignKey(p => p.ClusterId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
