// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core Konfiguration fuer die IndividualPeriod-Entity mit QueryFilter.
/// </summary>
using Klacks.Api.Domain.Models.Schedules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class IndividualPeriodConfiguration : IEntityTypeConfiguration<IndividualPeriod>
{
    public void Configure(EntityTypeBuilder<IndividualPeriod> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
