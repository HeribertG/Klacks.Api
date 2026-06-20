// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF configuration for ProactiveTriggerDispatchRow: table name, soft-delete query filter and a
/// unique index on (UserId, TriggerKind, DedupKey) so the same alert is recorded at most once.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class ProactiveTriggerDispatchRowConfiguration : IEntityTypeConfiguration<ProactiveTriggerDispatchRow>
{
    public void Configure(EntityTypeBuilder<ProactiveTriggerDispatchRow> builder)
    {
        builder.ToTable("agent_trigger_dispatches");
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.Property(p => p.UserId).HasMaxLength(64);
        builder.Property(p => p.TriggerKind).HasMaxLength(64);
        builder.Property(p => p.DedupKey).HasMaxLength(512);
        builder.HasIndex(p => new { p.UserId, p.TriggerKind, p.DedupKey })
            .IsUnique()
            .HasFilter("\"is_deleted\" = false");
    }
}
