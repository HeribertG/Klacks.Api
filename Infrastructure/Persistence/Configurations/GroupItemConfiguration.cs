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

        builder.HasIndex(p => new { p.ShiftId, p.GroupId })
            .IsUnique()
            .HasFilter("\"shift_id\" IS NOT NULL AND \"is_deleted\" = false");

        builder.HasIndex(p => new { p.ClientId, p.GroupId })
            .IsUnique()
            .HasFilter("\"client_id\" IS NOT NULL AND \"is_deleted\" = false");

        builder.HasOne(gi => gi.Shift)
            .WithMany(s => s.GroupItems)
            .HasForeignKey(gi => gi.ShiftId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
