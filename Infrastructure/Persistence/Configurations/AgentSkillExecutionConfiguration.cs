// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core Konfiguration fuer die AgentSkillExecution-Entity mit QueryFilter, Indizes und Skill-Beziehung.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class AgentSkillExecutionConfiguration : IEntityTypeConfiguration<AgentSkillExecution>
{
    public void Configure(EntityTypeBuilder<AgentSkillExecution> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => new { p.SessionId, p.CreateTime });
        builder.HasIndex(p => new { p.SkillId, p.CreateTime });

        builder.HasOne(e => e.Skill)
            .WithMany(s => s.Executions)
            .HasForeignKey(e => e.SkillId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
