// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for AgentTriggerGovernance: table name, soft-delete query filter and the two
/// partial unique indexes that keep one rule per scope. Two indexes rather than one over
/// (TriggerKind, GroupId), because Postgres treats NULLs in a unique index as distinct: a single index
/// would let the installation-wide rows - every one of which has a null GroupId - be inserted twice
/// over. The alternative, NULLS NOT DISTINCT, is what SkillPhraseConfiguration deliberately moved away
/// from, so the split filter is used instead.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class AgentTriggerGovernanceConfiguration : IEntityTypeConfiguration<AgentTriggerGovernance>
{
    private const int TriggerKindMaxLength = 64;
    private const string GlobalRuleFilter = "\"group_id\" IS NULL AND \"is_deleted\" = false";
    private const string GroupRuleFilter = "\"group_id\" IS NOT NULL AND \"is_deleted\" = false";

    public void Configure(EntityTypeBuilder<AgentTriggerGovernance> builder)
    {
        builder.ToTable("agent_trigger_governance");
        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.Property(p => p.TriggerKind).HasMaxLength(TriggerKindMaxLength).IsRequired();

        builder.HasIndex(p => p.TriggerKind)
            .IsUnique()
            .HasFilter(GlobalRuleFilter)
            .HasDatabaseName("ix_agent_trigger_governance_trigger_kind_global");

        builder.HasIndex(p => new { p.TriggerKind, p.GroupId })
            .IsUnique()
            .HasFilter(GroupRuleFilter)
            .HasDatabaseName("ix_agent_trigger_governance_trigger_kind_group");
    }
}
