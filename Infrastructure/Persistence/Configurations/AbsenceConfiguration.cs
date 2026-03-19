// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core Konfiguration fuer die Absence-Entity mit QueryFilter, MultiLanguage-Properties und Index.
/// </summary>
using Klacks.Api.Domain.Models.Schedules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class AbsenceConfiguration : IEntityTypeConfiguration<Absence>
{
    public void Configure(EntityTypeBuilder<Absence> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
        builder.ConfigureMultiLanguage(a => a.Name, "name");
        builder.ConfigureMultiLanguage(a => a.Description, "description");
        builder.ConfigureMultiLanguage(a => a.Abbreviation, "abbreviation");
        builder.HasIndex(p => new { p.IsDeleted });
    }
}
