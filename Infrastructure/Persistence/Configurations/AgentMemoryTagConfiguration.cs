// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// EF Core configuration for the AgentMemoryTag entity with composite key, index and Memory relationship.
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
