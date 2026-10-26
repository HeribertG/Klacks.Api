// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// EF Core configuration of the per-address geocoding state: one row per address (key = address id, removed with
/// the address), table name pinned because AddressGeocodingStateRepository writes it with upsert SQL.
/// </summary>

using Klacks.Api.Domain.Models.Geocoding;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Repositories.Geocoding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Klacks.Api.Infrastructure.Persistence.Configurations;

public class AddressGeocodingStateConfiguration : IEntityTypeConfiguration<AddressGeocodingState>
{
    public void Configure(EntityTypeBuilder<AddressGeocodingState> builder)
    {
        builder.ToTable(GeocodingTableNames.AddressGeocodingState);
        builder.HasKey(state => state.AddressId);
        builder.HasOne<Address>()
            .WithOne()
            .HasForeignKey<AddressGeocodingState>(state => state.AddressId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(state => state.RequestedAt);
    }
}
