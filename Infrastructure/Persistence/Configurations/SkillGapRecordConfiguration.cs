// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core Konfiguration fuer die SkillGapRecord-Entity mit Tabellenname, QueryFilter und Indizes.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class SkillGapRecordConfiguration : IEntityTypeConfiguration<SkillGapRecord>
{
    public void Configure(EntityTypeBuilder<SkillGapRecord> builder)
    {
        builder.ToTable("skill_gap_records");
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => new { p.AgentId, p.Status });
        builder.HasIndex(p => new { p.AgentId, p.OccurrenceCount });
    }
}
