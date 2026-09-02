// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for SkillUsageRecord. Pins the table name and adds the soft-delete query
/// filter the entity was missing, so a deleted telemetry row can no longer inflate the failure
/// counters of the "Skill-Wirksamkeit" scorecard (W6). The filter produces no schema change, so no
/// migration belongs to this file.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class SkillUsageRecordConfiguration : IEntityTypeConfiguration<SkillUsageRecord>
{
    private const string TableName = "skill_usage_records";

    public void Configure(EntityTypeBuilder<SkillUsageRecord> builder)
    {
        builder.ToTable(TableName);
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
