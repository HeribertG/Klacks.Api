// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core Konfiguration fuer die AgentSkill-Entity mit QueryFilter, Indizes, JSONB-Synonyms und Agent-Beziehung.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class AgentSkillConfiguration : IEntityTypeConfiguration<AgentSkill>
{
    public void Configure(EntityTypeBuilder<AgentSkill> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => new { p.AgentId, p.Name })
            .HasFilter("is_deleted = false")
            .IsUnique();
        builder.HasIndex(p => new { p.AgentId, p.IsEnabled, p.SortOrder });
        builder.Property(e => e.Synonyms)
            .HasJsonbConversionWithComparer<Dictionary<string, List<string>>>();

        builder.HasOne(s => s.Agent)
            .WithMany(a => a.Skills)
            .HasForeignKey(s => s.AgentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
