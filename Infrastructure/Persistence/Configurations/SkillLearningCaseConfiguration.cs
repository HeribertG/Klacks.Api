// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for SkillLearningCase. Cases die with their cluster, so the foreign key
/// cascades; the index on user_id serves both the distinct-user threshold and UserDataEraser.
/// </summary>
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class SkillLearningCaseConfiguration : IEntityTypeConfiguration<SkillLearningCase>
{
    private const int IntentExcerptMaxLength = SkillLearningDefaults.ExcerptMaxLength;
    private const int LocaleMaxLength = 8;
    private const int SignalMaxLength = 24;
    private const int SkillNameMaxLength = 128;

    public void Configure(EntityTypeBuilder<SkillLearningCase> builder)
    {
        builder.ToTable("skill_learning_cases");
        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.Property(p => p.IntentExcerpt).HasMaxLength(IntentExcerptMaxLength);
        builder.Property(p => p.Locale).HasMaxLength(LocaleMaxLength);
        builder.Property(p => p.Signal).HasMaxLength(SignalMaxLength).IsRequired();
        builder.Property(p => p.ChosenSkill).HasMaxLength(SkillNameMaxLength);
        builder.Property(p => p.ExpectedSkill).HasMaxLength(SkillNameMaxLength);
        builder.Property(p => p.ToolsetJson).HasColumnType("jsonb");

        builder.HasIndex(p => new { p.ClusterId, p.OccurredAtUtc });
        builder.HasIndex(p => p.UserId);

        builder.HasOne<SkillLearningCluster>()
            .WithMany()
            .HasForeignKey(p => p.ClusterId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
