// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core Konfiguration fuer die AgentSoulSection-Entity mit QueryFilter, Indizes und Agent-Beziehung.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class AgentSoulSectionConfiguration : IEntityTypeConfiguration<AgentSoulSection>
{
    public void Configure(EntityTypeBuilder<AgentSoulSection> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => new { p.AgentId, p.SectionType })
            .HasFilter("is_active = true AND is_deleted = false")
            .IsUnique();
        builder.HasIndex(p => new { p.AgentId, p.SortOrder })
            .HasFilter("is_active = true AND is_deleted = false");

        builder.HasOne(s => s.Agent)
            .WithMany(a => a.SoulSections)
            .HasForeignKey(s => s.AgentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
