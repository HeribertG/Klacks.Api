// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF configuration for ProactiveTriggerDispatchRow: table name, soft-delete query filter and a
/// unique index on (UserId, TriggerKind, DedupKey) so the same alert is recorded at most once.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class ProactiveTriggerDispatchRowConfiguration : IEntityTypeConfiguration<ProactiveTriggerDispatchRow>
{
    private const int UserIdMaxLength = 64;
    private const int TriggerKindMaxLength = 64;
    private const int DedupKeyMaxLength = 512;
    private const int ContentKeyMaxLength = 512;
    private const int SeverityMaxLength = 16;

    public void Configure(EntityTypeBuilder<ProactiveTriggerDispatchRow> builder)
    {
        builder.ToTable("agent_trigger_dispatches");
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.Property(p => p.UserId).HasMaxLength(UserIdMaxLength);
        builder.Property(p => p.TriggerKind).HasMaxLength(TriggerKindMaxLength);
        builder.Property(p => p.DedupKey).HasMaxLength(DedupKeyMaxLength);
        builder.Property(p => p.ContentKey).HasMaxLength(ContentKeyMaxLength);
        builder.Property(p => p.ContentParamsJson).HasMaxLength(ProactiveTriggerDispatchLimits.ContentParamsJsonMaxLength);
        builder.Property(p => p.Severity).HasMaxLength(SeverityMaxLength);
        builder.Property(p => p.ActionRoute).HasMaxLength(ProactiveTriggerDispatchLimits.ActionRouteMaxLength);
        builder.Property(p => p.ActionParamsJson).HasMaxLength(ProactiveTriggerDispatchLimits.ActionParamsJsonMaxLength);
        builder.HasIndex(p => new { p.UserId, p.TriggerKind, p.DedupKey })
            .IsUnique()
            .HasFilter("\"is_deleted\" = false");
    }
}
