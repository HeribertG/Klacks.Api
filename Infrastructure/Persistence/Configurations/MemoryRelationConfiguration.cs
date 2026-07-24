// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for MemoryRelation: soft-delete query filter, per-agent FKs to both endpoint
/// memories and a partial unique index over (agent, memory A, memory B, type) restricted to
/// non-deleted rows. The table is explicitly named in the singular ("memory_relation") per project
/// convention for this feature, overriding the pluralizing snake_case naming convention.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class MemoryRelationConfiguration : IEntityTypeConfiguration<MemoryRelation>
{
    public void Configure(EntityTypeBuilder<MemoryRelation> builder)
    {
        builder.ToTable("memory_relation");

        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.HasIndex(p => new { p.AgentId, p.Status });
        builder.HasIndex(p => p.MemoryAId);
        builder.HasIndex(p => p.MemoryBId);

        builder.HasIndex(p => new { p.AgentId, p.MemoryAId, p.MemoryBId, p.Type })
            .HasFilter("is_deleted = false")
            .IsUnique();

        builder.HasOne(r => r.Agent)
            .WithMany()
            .HasForeignKey(r => r.AgentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.MemoryA)
            .WithMany()
            .HasForeignKey(r => r.MemoryAId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_memory_relation_agent_memories_memory_a_id");

        builder.HasOne(r => r.MemoryB)
            .WithMany()
            .HasForeignKey(r => r.MemoryBId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_memory_relation_agent_memories_memory_b_id");
    }
}
