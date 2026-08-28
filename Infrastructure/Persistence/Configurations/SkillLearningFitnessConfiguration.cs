// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for SkillLearningFitness, one row per activated candidate and calendar week.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class SkillLearningFitnessConfiguration : IEntityTypeConfiguration<SkillLearningFitness>
{
    private const int QuotePrecision = 5;
    private const int QuoteScale = 4;

    private const string ActiveRowFilter = "\"is_deleted\" = false";

    public void Configure(EntityTypeBuilder<SkillLearningFitness> builder)
    {
        builder.ToTable("skill_learning_fitness");
        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.Property(p => p.Quote).HasPrecision(QuotePrecision, QuoteScale);

        builder.HasIndex(p => new { p.CandidateId, p.WindowStartUtc })
            .IsUnique()
            .HasFilter(ActiveRowFilter);

        builder.HasOne<SkillLearningCandidate>()
            .WithMany()
            .HasForeignKey(p => p.CandidateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
