// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for AgentCondition: table name, soft-delete query filter, a partial unique
/// index on Fingerprint restricted to open statuses (multi-instance-safe re-arm), lookup indexes and
/// the two FK relationships (ScenarioId, self-referencing CausedByConditionId), both non-cascading so a
/// deleted scenario or cascade condition never silently drops ledger history.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Schedules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class AgentConditionConfiguration : IEntityTypeConfiguration<AgentCondition>
{
    private const int TriggerKindMaxLength = 64;
    private const int FingerprintMaxLength = 512;
    private const int SeverityMaxLength = 16;

    private static readonly string OpenFingerprintFilter =
        "\"is_deleted\" = false AND \"status\" NOT IN (" +
        string.Join(", ", AgentConditionStateMachine.TerminalStatuses
            .Select(status => (int)status)
            .OrderBy(value => value)) + ")";

    public void Configure(EntityTypeBuilder<AgentCondition> builder)
    {
        builder.ToTable("agent_conditions");
        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.Property(p => p.TriggerKind).HasMaxLength(TriggerKindMaxLength);
        builder.Property(p => p.Fingerprint).HasMaxLength(FingerprintMaxLength);
        builder.Property(p => p.Severity).HasMaxLength(SeverityMaxLength);
        builder.Property(p => p.PayloadJson).IsRequired();

        builder.HasIndex(p => p.Fingerprint)
            .IsUnique()
            .HasFilter(OpenFingerprintFilter);
        builder.HasIndex(p => new { p.Status, p.TriggerKind });
        builder.HasIndex(p => p.GroupId);

        builder.HasOne<AnalyseScenario>()
            .WithMany()
            .HasForeignKey(p => p.ScenarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AgentCondition>()
            .WithMany()
            .HasForeignKey(p => p.CausedByConditionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
