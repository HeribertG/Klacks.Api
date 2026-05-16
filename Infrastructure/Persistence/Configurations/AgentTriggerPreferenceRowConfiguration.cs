// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF configuration for AgentTriggerPreferenceRow: table name, soft-delete query filter,
/// unique index on (UserId, TriggerKind) so each pair is stored at most once.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class AgentTriggerPreferenceRowConfiguration : IEntityTypeConfiguration<AgentTriggerPreferenceRow>
{
    public void Configure(EntityTypeBuilder<AgentTriggerPreferenceRow> builder)
    {
        builder.ToTable("agent_trigger_preferences");
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.Property(p => p.UserId).HasMaxLength(64);
        builder.Property(p => p.TriggerKind).HasMaxLength(64);
        builder.Property(p => p.MinimumSeverity).HasMaxLength(16);
        builder.HasIndex(p => new { p.UserId, p.TriggerKind })
            .IsUnique()
            .HasFilter("\"is_deleted\" = false");
    }
}
