// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core Konfiguration fuer die AgentLink-Entity mit QueryFilter, eindeutigem Index und Beziehungen.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class AgentLinkConfiguration : IEntityTypeConfiguration<AgentLink>
{
    public void Configure(EntityTypeBuilder<AgentLink> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => new { p.SourceAgentId, p.TargetAgentId, p.LinkType })
            .HasFilter("is_active = true AND is_deleted = false")
            .IsUnique();

        builder.HasOne(l => l.SourceAgent)
            .WithMany()
            .HasForeignKey(l => l.SourceAgentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.TargetAgent)
            .WithMany()
            .HasForeignKey(l => l.TargetAgentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
