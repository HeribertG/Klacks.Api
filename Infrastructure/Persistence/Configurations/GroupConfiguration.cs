// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core Konfiguration fuer die Group-Entity mit QueryFilter, Indizes und Beziehungen.
/// </summary>
using Klacks.Api.Domain.Models.Associations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => new { p.Name });

        builder.HasMany(g => g.GroupItems)
            .WithOne(gi => gi.Group)
            .HasForeignKey(gi => gi.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(g => g.CalendarSelection)
            .WithMany()
            .HasForeignKey(g => g.CalendarSelectionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
