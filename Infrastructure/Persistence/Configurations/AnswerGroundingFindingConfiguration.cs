// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for AnswerGroundingFinding with table name, query filter and indexes.
/// </summary>

using Klacks.Api.Domain.Models.Assistant.Grounding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class AnswerGroundingFindingConfiguration : IEntityTypeConfiguration<AnswerGroundingFinding>
{
    public void Configure(EntityTypeBuilder<AnswerGroundingFinding> builder)
    {
        builder.ToTable("answer_grounding_findings");
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => new { p.AgentId, p.CreateTime });
        builder.HasIndex(p => new { p.EvaluatorVersion, p.Tier });
    }
}
