// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for StandingApproval: table name, soft-delete query filter and the lookup index
/// the action dispatcher reads through.
///
/// NO foreign key to group and none to AspNetUsers, exactly as AgentCondition and AgentTriggerGovernance
/// have none. Both omissions are load-bearing, not oversights. The retention purge
/// (DataRetentionBackgroundService) physically deletes soft-deleted group rows with a raw DELETE per
/// table, and a restricting foreign key from here would make that whole statement fail for as long as one
/// grant referenced such a group - the purge swallows the error per table, so group retention would
/// silently never run again. AppUser.Id is text and AppUser is the one entity without soft delete, so a
/// Guid user column cannot carry a foreign key to it in the first place.
///
/// There is deliberately NO unique index over (TriggerKind, GroupId). "At most one grant per scope" is
/// true only of ACTIVE grants, and activity depends on the current time, which no index can express: a
/// partial unique index over the non-revoked rows would let an expired grant permanently block a fresh
/// one for the same scope. The duplicate check therefore lives in GrantStandingApprovalCommandHandler,
/// and the resolver takes the newest row when a race produced two.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class StandingApprovalConfiguration : IEntityTypeConfiguration<StandingApproval>
{
    public void Configure(EntityTypeBuilder<StandingApproval> builder)
    {
        builder.ToTable("agent_standing_approval");
        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.Property(p => p.TriggerKind)
            .HasMaxLength(StandingApprovalDefaults.TriggerKindMaxLength)
            .IsRequired();

        builder.HasIndex(p => new { p.TriggerKind, p.GroupId })
            .HasDatabaseName("ix_agent_standing_approval_trigger_kind_group");
    }
}
