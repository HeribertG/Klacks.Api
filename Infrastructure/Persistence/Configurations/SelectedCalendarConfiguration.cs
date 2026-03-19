// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core Konfiguration fuer die SelectedCalendar-Entity mit QueryFilter, Index und CalendarSelection-Beziehung.
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
