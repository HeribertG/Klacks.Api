// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the Group-Entity with query filter, indexes and relationshipen.
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
