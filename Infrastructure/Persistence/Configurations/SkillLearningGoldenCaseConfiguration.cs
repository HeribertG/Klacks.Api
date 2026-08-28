// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for SkillLearningGoldenCase. The foreign key sets null instead of cascading:
/// a golden case must outlive the cluster it came from, otherwise retention would delete exactly the
/// regression protection the loop built up.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class SkillLearningGoldenCaseConfiguration : IEntityTypeConfiguration<SkillLearningGoldenCase>
{
    private const int QueryMaxLength = 120;
    private const int LocaleMaxLength = 8;
    private const int ExpectedSourceIdMaxLength = 128;

    public void Configure(EntityTypeBuilder<SkillLearningGoldenCase> builder)
    {
        builder.ToTable("skill_learning_golden_cases");
        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.Property(p => p.Query).HasMaxLength(QueryMaxLength).IsRequired();
        builder.Property(p => p.Locale).HasMaxLength(LocaleMaxLength);
        builder.Property(p => p.ExpectedSourceId).HasMaxLength(ExpectedSourceIdMaxLength).IsRequired();

        builder.HasIndex(p => p.ExpectedSourceId);

        builder.HasOne<SkillLearningCluster>()
            .WithMany()
            .HasForeignKey(p => p.ClusterId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
