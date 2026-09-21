// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for AgentConditionGroup: table name, the composite key over both columns (which
/// is also the uniqueness guarantee that makes the ledger service's set diff idempotent), a lookup index
/// on GroupId for the reverse direction, and the cascading FK to the owning AgentCondition.
///
/// Three deliberate omissions. No soft-delete query filter, because the entity carries no IsDeleted - it
/// is reached only through its condition, whose own filter already hides a soft-deleted row. Cascade
/// rather than Restrict on the condition FK, because the retention purge deletes expired conditions with
/// a raw DELETE and a Restrict FK would make that statement fail for the whole table. And no FK from
/// GroupId to Group, exactly as AgentCondition.GroupId has none: the same purge physically deletes
/// soft-deleted group rows, and a Restrict FK there would break that in turn.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class AgentConditionGroupConfiguration : IEntityTypeConfiguration<AgentConditionGroup>
{
    public void Configure(EntityTypeBuilder<AgentConditionGroup> builder)
    {
        builder.ToTable("agent_condition_groups");

        builder.HasKey(p => new { p.ConditionId, p.GroupId });
        builder.HasIndex(p => p.GroupId);

        builder.HasOne<AgentCondition>()
            .WithMany(c => c.Groups)
            .HasForeignKey(p => p.ConditionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
