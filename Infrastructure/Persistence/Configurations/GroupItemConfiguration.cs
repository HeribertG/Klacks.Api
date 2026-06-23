// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the GroupItem-Entity with query filter, indexes and Shift-relationship.
/// </summary>
using Klacks.Api.Domain.Models.Associations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class GroupItemConfiguration : IEntityTypeConfiguration<GroupItem>
{
    public void Configure(EntityTypeBuilder<GroupItem> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => new { p.GroupId, p.ClientId, p.IsDeleted });
        builder.HasIndex(p => new { p.ClientId, p.GroupId, p.ShiftId });

        // Real-membership uniqueness only (analyse_token IS NULL): a scenario membership (token != null)
        // may coexist with the real one for the same (client/shift, group) without colliding. The filter
        // must add "analyse_token IS NULL" rather than putting analyse_token into the columns — Postgres
        // treats NULLs as distinct, which would let duplicate real memberships slip in.
        builder.HasIndex(p => new { p.ShiftId, p.GroupId })
            .IsUnique()
            .HasFilter("\"shift_id\" IS NOT NULL AND \"is_deleted\" = false AND \"analyse_token\" IS NULL");

        builder.HasIndex(p => new { p.ClientId, p.GroupId })
            .IsUnique()
            .HasFilter("\"client_id\" IS NOT NULL AND \"is_deleted\" = false AND \"analyse_token\" IS NULL");

        // Scenario-membership uniqueness: at most one temporary client membership per (client, group, token).
        builder.HasIndex(p => new { p.ClientId, p.GroupId, p.AnalyseToken })
            .IsUnique()
            .HasFilter("\"client_id\" IS NOT NULL AND \"is_deleted\" = false AND \"analyse_token\" IS NOT NULL");

        builder.HasOne(gi => gi.Shift)
            .WithMany(s => s.GroupItems)
            .HasForeignKey(gi => gi.ShiftId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
