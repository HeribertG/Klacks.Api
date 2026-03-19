// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core Konfiguration fuer die AgentSoulHistory-Entity mit QueryFilter, Indizes und SoulSection-Beziehung.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class AgentSoulHistoryConfiguration : IEntityTypeConfiguration<AgentSoulHistory>
{
    public void Configure(EntityTypeBuilder<AgentSoulHistory> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => new { p.SoulSectionId, p.CreateTime });
        builder.HasIndex(p => new { p.AgentId, p.CreateTime });

        builder.HasOne(h => h.SoulSection)
            .WithMany(s => s.History)
            .HasForeignKey(h => h.SoulSectionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
