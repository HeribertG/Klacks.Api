// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for SkillRelation: soft-delete query filter, per-agent FK and a partial
/// unique index over (agent, skill A, skill B, type) restricted to non-deleted rows.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class SkillRelationConfiguration : IEntityTypeConfiguration<SkillRelation>
{
    public void Configure(EntityTypeBuilder<SkillRelation> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.HasIndex(p => new { p.AgentId, p.Status });

        builder.HasIndex(p => new { p.AgentId, p.SkillAName, p.SkillBName, p.Type })
            .HasFilter("is_deleted = false")
            .IsUnique();

        builder.HasOne(r => r.Agent)
            .WithMany()
            .HasForeignKey(r => r.AgentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
