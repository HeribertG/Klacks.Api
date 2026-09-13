// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for SkillLearningGoldenCase. The foreign key sets null instead of cascading:
/// a golden case must outlive the cluster it came from, otherwise retention would delete exactly the
/// regression protection the loop built up. The origin/partition index serves the holdout lookup every
/// gate runs before it decides anything.
/// </summary>
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class SkillLearningGoldenCaseConfiguration : IEntityTypeConfiguration<SkillLearningGoldenCase>
{
    private const int QueryMaxLength = SkillLearningDefaults.ExcerptMaxLength;
    private const int LocaleMaxLength = 8;
    private const int ExpectedSourceIdMaxLength = 128;
    private const int OriginMaxLength = 16;
    private const int PartitionMaxLength = 16;

    public void Configure(EntityTypeBuilder<SkillLearningGoldenCase> builder)
    {
        builder.ToTable("skill_learning_golden_cases");
        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.Property(p => p.Query).HasMaxLength(QueryMaxLength).IsRequired();
        builder.Property(p => p.Locale).HasMaxLength(LocaleMaxLength);
        builder.Property(p => p.ExpectedSourceId).HasMaxLength(ExpectedSourceIdMaxLength).IsRequired();
        builder.Property(p => p.Origin)
            .HasMaxLength(OriginMaxLength)
            .HasDefaultValue(GoldenCaseOrigins.Cluster)
            .IsRequired();
        builder.Property(p => p.Partition)
            .HasMaxLength(PartitionMaxLength)
            .HasDefaultValue(GoldenCasePartitions.Holdout)
            .IsRequired();

        builder.HasIndex(p => p.ExpectedSourceId);
        builder.HasIndex(p => new { p.Origin, p.Partition });

        builder.HasOne<SkillLearningCluster>()
            .WithMany()
            .HasForeignKey(p => p.ClusterId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
