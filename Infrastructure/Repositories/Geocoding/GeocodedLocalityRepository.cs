// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Persistent locality geocoding cache (self-committing). StoreAsync is an INSERT ... ON CONFLICT upsert that keeps
/// the newer answer, so concurrent writers for the same locality never fail and an expired "not found" is replaced
/// by a later hit.
/// </summary>
/// <param name="context">Database context</param>

using Klacks.Api.Application.Interfaces.Geocoding;
using Klacks.Api.Domain.Models.Geocoding;
using Klacks.Api.Domain.Services.Geocoding;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;

namespace Klacks.Api.Infrastructure.Repositories.Geocoding;

public class GeocodedLocalityRepository : IGeocodedLocalityRepository
{
    private const string UpsertSql =
        $"INSERT INTO {GeocodingTableNames.GeocodedLocality} " +
        "(country_code, postal_code, city, found, latitude, longitude, state, county, resolved_at) " +
        "VALUES (@country_code, @postal_code, @city, @found, @latitude, @longitude, @state, @county, @resolved_at) " +
        "ON CONFLICT (country_code, postal_code, city) DO UPDATE SET found = EXCLUDED.found, latitude = EXCLUDED.latitude, " +
        "longitude = EXCLUDED.longitude, state = EXCLUDED.state, county = EXCLUDED.county, resolved_at = EXCLUDED.resolved_at " +
        $"WHERE {GeocodingTableNames.GeocodedLocality}.resolved_at <= EXCLUDED.resolved_at";

    private readonly DataBaseContext _context;

    public GeocodedLocalityRepository(DataBaseContext context)
    {
        _context = context;
    }

    public Task<GeocodedLocality?> GetAsync(GeocodingLocalityKey key, CancellationToken cancellationToken = default) =>
        _context.GeocodedLocalities
            .AsNoTracking()
            .FirstOrDefaultAsync(
                locality => locality.CountryCode == key.CountryCode && locality.PostalCode == key.PostalCode && locality.City == key.City,
                cancellationToken);

    public async Task StoreAsync(GeocodedLocality locality, CancellationToken cancellationToken = default)
    {
        await _context.Database.ExecuteSqlRawAsync(
            UpsertSql,
            [
                GeocodingSqlParameter.Create("country_code", NpgsqlDbType.Text, locality.CountryCode),
                GeocodingSqlParameter.Create("postal_code", NpgsqlDbType.Text, locality.PostalCode),
                GeocodingSqlParameter.Create("city", NpgsqlDbType.Text, locality.City),
                GeocodingSqlParameter.Create("found", NpgsqlDbType.Boolean, locality.Found),
                GeocodingSqlParameter.Create("latitude", NpgsqlDbType.Double, locality.Latitude),
                GeocodingSqlParameter.Create("longitude", NpgsqlDbType.Double, locality.Longitude),
                GeocodingSqlParameter.Create("state", NpgsqlDbType.Text, locality.State),
                GeocodingSqlParameter.Create("county", NpgsqlDbType.Text, locality.County),
                GeocodingSqlParameter.Create("resolved_at", NpgsqlDbType.TimestampTz, locality.ResolvedAt)
            ],
            cancellationToken);
    }
}
