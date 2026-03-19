// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core Konfiguration fuer die CalendarSelection-Entity mit QueryFilter.
/// </summary>
using Klacks.Api.Domain.Models.CalendarSelections;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class CalendarSelectionConfiguration : IEntityTypeConfiguration<CalendarSelection>
{
    public void Configure(EntityTypeBuilder<CalendarSelection> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
