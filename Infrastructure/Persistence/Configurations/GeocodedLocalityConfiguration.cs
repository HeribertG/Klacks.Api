// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// EF Core configuration of the persistent locality geocoding cache: composite key country code, postcode and city
/// (normalized by GeocodingLocalityKey), table name pinned because GeocodedLocalityRepository upserts with SQL.
/// </summary>

using Klacks.Api.Domain.Models.Geocoding;
using Klacks.Api.Infrastructure.Repositories.Geocoding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class GeocodedLocalityConfiguration : IEntityTypeConfiguration<GeocodedLocality>
{
    public void Configure(EntityTypeBuilder<GeocodedLocality> builder)
    {
        builder.ToTable(GeocodingTableNames.GeocodedLocality);
        builder.HasKey(locality => new { locality.CountryCode, locality.PostalCode, locality.City });
    }
}
