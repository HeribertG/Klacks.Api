// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF configuration for ProactiveTriggerDispatchRow: table name, soft-delete query filter and the
/// dedup indexes. The single unique index on (UserId, TriggerKind, DedupKey) was split into two
/// partial unique indexes because Postgres treats NULLs as distinct: rows without a ConditionId and
/// rows with one are deduplicated separately, so a linked row never collides with an unlinked one
/// that shares the same content key. A third partial index on NextReminderAtUtc feeds the reminder
/// sweep. RejectReason needs no explicit mapping - EF stores the nullable enum in the integer column
/// it would pick for a nullable int, exactly as AgentCondition.DelegatedMaxAction is stored.
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
            .HasDatabaseName("ix_agent_trigger_dispatches_dedup_unlinked")
            .HasFilter("\"is_deleted\" = false AND \"condition_id\" IS NULL");
        builder.HasIndex(p => new { p.UserId, p.TriggerKind, p.DedupKey, p.ConditionId })
            .IsUnique()
            .HasDatabaseName("ix_agent_trigger_dispatches_dedup_linked")
            .HasFilter("\"is_deleted\" = false AND \"condition_id\" IS NOT NULL");
        builder.HasIndex(p => p.NextReminderAtUtc)
            .HasDatabaseName("ix_agent_trigger_dispatches_reminder_due")
            .HasFilter("\"next_reminder_at_utc\" IS NOT NULL AND \"is_deleted\" = false");
    }
}
