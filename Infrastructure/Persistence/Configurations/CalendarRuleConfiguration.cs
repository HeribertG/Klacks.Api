// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the CalendarRule entity with index and MultiLanguage properties.
/// </summary>
using Klacks.Api.Domain.Models.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class CalendarRuleConfiguration : IEntityTypeConfiguration<CalendarRule>
{
    public void Configure(EntityTypeBuilder<CalendarRule> builder)
    {
        builder.HasIndex(p => new { p.State, p.Country });
        builder.ConfigureMultiLanguage(c => c.Name, "name");
        builder.ConfigureMultiLanguage(c => c.Description, "description");
    }
}
