// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for ProposedSkillChange with table name, query filter and indexes.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class ProposedSkillChangeConfiguration : IEntityTypeConfiguration<ProposedSkillChange>
{
    public void Configure(EntityTypeBuilder<ProposedSkillChange> builder)
    {
        builder.ToTable("proposed_skill_changes");
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => new { p.SkillId, p.Field, p.Status });
        builder.Property(p => p.SkillName).HasMaxLength(128);
        builder.Property(p => p.Field).HasMaxLength(32);
        builder.Property(p => p.Status).HasMaxLength(16);
        builder.Property(p => p.ReviewedBy).HasMaxLength(128);
        builder.Property(p => p.EvidenceJson).HasColumnType("jsonb");
    }
}
