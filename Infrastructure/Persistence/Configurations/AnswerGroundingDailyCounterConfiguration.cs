// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for AnswerGroundingDailyCounter; the unique index backs the
/// ON CONFLICT upsert of the repository.
/// </summary>

using Klacks.Api.Domain.Models.Assistant.Grounding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class AnswerGroundingDailyCounterConfiguration : IEntityTypeConfiguration<AnswerGroundingDailyCounter>
{
    public void Configure(EntityTypeBuilder<AnswerGroundingDailyCounter> builder)
    {
        builder.ToTable("answer_grounding_daily_counters");
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => new { p.Day, p.AgentId, p.EvaluatorVersion })
            .IsUnique()
            .HasFilter("\"is_deleted\" = false");
    }
}
