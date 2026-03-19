// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core Konfiguration fuer die AgentSession-Entity mit QueryFilter, Indizes und Agent-Beziehung.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class AgentSessionConfiguration : IEntityTypeConfiguration<AgentSession>
{
    public void Configure(EntityTypeBuilder<AgentSession> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => new { p.AgentId, p.Status });
        builder.HasIndex(p => new { p.UserId, p.UpdateTime });
        builder.HasIndex(p => new { p.AgentId, p.SessionId }).IsUnique();

        builder.HasOne(s => s.Agent)
            .WithMany(a => a.Sessions)
            .HasForeignKey(s => s.AgentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
