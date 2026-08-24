// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for AgentConditionEvent: table name, soft-delete query filter and the
/// cascading FK to its owning AgentCondition (deleting a condition's audit trail along with the
/// condition is correct here; soft-delete means the cascade never actually fires in practice).
/// </summary>

using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class AgentConditionEventConfiguration : IEntityTypeConfiguration<AgentConditionEvent>
{
    private const int EventTypeMaxLength = 64;

    public void Configure(EntityTypeBuilder<AgentConditionEvent> builder)
    {
        builder.ToTable("agent_condition_events");
        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.Property(p => p.EventType).HasMaxLength(EventTypeMaxLength);

        builder.HasOne<AgentCondition>()
            .WithMany()
            .HasForeignKey(p => p.ConditionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
