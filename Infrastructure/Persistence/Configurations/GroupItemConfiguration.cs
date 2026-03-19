// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core Konfiguration fuer die GroupItem-Entity mit QueryFilter, Indizes und Shift-Beziehung.
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

        builder.HasOne(gi => gi.Shift)
            .WithMany(s => s.GroupItems)
            .HasForeignKey(gi => gi.ShiftId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
