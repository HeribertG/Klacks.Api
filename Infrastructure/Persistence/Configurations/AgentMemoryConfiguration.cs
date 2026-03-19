// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core Konfiguration fuer die AgentMemory-Entity mit QueryFilter, Indizes und Beziehungen.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class AgentMemoryConfiguration : IEntityTypeConfiguration<AgentMemory>
{
    public void Configure(EntityTypeBuilder<AgentMemory> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => new { p.AgentId, p.Category });
        builder.HasIndex(p => new { p.AgentId, p.Importance });
        builder.HasIndex(p => p.AgentId).HasFilter("is_pinned = true AND is_deleted = false");
        builder.HasIndex(p => p.ExpiresAt).HasFilter("expires_at IS NOT NULL AND is_deleted = false");
        builder.HasIndex(p => new { p.AgentId, p.Source });

        builder.HasOne(m => m.Agent)
            .WithMany(a => a.Memories)
            .HasForeignKey(m => m.AgentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Supersedes)
            .WithMany()
            .HasForeignKey(m => m.SupersedesId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
