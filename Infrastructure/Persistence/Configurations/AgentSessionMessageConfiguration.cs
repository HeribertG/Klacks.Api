// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core Konfiguration fuer die AgentSessionMessage-Entity mit QueryFilter, Indizes und Beziehungen.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class AgentSessionMessageConfiguration : IEntityTypeConfiguration<AgentSessionMessage>
{
    public void Configure(EntityTypeBuilder<AgentSessionMessage> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => new { p.SessionId, p.CreateTime });
        builder.HasIndex(p => new { p.SessionId, p.CreateTime })
            .HasFilter("is_compacted = false AND is_deleted = false");

        builder.HasOne(m => m.Session)
            .WithMany(s => s.Messages)
            .HasForeignKey(m => m.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.CompactedInto)
            .WithMany()
            .HasForeignKey(m => m.CompactedIntoId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
