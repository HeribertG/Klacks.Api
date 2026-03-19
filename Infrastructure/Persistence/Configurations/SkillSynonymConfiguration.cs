// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core Konfiguration fuer die SkillSynonym-Entity mit Tabellenname, QueryFilter und Indizes.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class SkillSynonymConfiguration : IEntityTypeConfiguration<SkillSynonym>
{
    public void Configure(EntityTypeBuilder<SkillSynonym> builder)
    {
        builder.ToTable("skill_synonyms");
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => p.Language);
        builder.HasIndex(p => new { p.SkillName, p.Language, p.Keyword })
            .HasFilter("is_deleted = false")
            .IsUnique();
    }
}
