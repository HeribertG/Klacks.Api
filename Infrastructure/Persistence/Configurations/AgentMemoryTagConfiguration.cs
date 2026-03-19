// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core Konfiguration fuer die AgentMemoryTag-Entity mit zusammengesetztem Key, Index und Memory-Beziehung.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class AgentMemoryTagConfiguration : IEntityTypeConfiguration<AgentMemoryTag>
{
    public void Configure(EntityTypeBuilder<AgentMemoryTag> builder)
    {
        builder.HasKey(t => new { t.MemoryId, t.Tag });
        builder.HasIndex(t => t.Tag);

        builder.HasOne(t => t.Memory)
            .WithMany(m => m.Tags)
            .HasForeignKey(t => t.MemoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
