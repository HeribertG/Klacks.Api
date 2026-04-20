// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core configuration for the ClientPeriodHours-Entity with unique index and query filter.
/// </summary>
using Klacks.Api.Domain.Models.Schedules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class ClientPeriodHoursConfiguration : IEntityTypeConfiguration<ClientPeriodHours>
{
    public void Configure(EntityTypeBuilder<ClientPeriodHours> builder)
    {
        builder.HasIndex(p => new { p.ClientId, p.StartDate, p.EndDate, p.AnalyseToken })
            .IsUnique();

        builder.HasQueryFilter(p => !p.Client!.IsDeleted);
    }
}
