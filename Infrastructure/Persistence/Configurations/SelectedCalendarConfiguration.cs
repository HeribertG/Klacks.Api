// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the SelectedCalendar-Entity with query filter, Index and CalendarSelection-relationship.
/// </summary>
using Klacks.Api.Domain.Models.CalendarSelections;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class SelectedCalendarConfiguration : IEntityTypeConfiguration<SelectedCalendar>
{
    public void Configure(EntityTypeBuilder<SelectedCalendar> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.HasIndex(p => new { p.State, p.Country, p.CalendarSelectionId });

        builder.HasOne(p => p.CalendarSelection)
            .WithMany(b => b.SelectedCalendars)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
