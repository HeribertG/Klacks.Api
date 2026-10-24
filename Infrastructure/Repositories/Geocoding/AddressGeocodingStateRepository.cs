// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads geocoding candidates and writes coordinates, attempts and requests (self-committing, see
/// IAddressGeocodingStateRepository). Only addresses with neither latitude nor longitude are candidates: one with a
/// single coordinate is left as it is, because the fill never overwrites. The coordinate write is one ExecuteUpdate
/// whose WHERE pins id, empty coordinates and the geocoded postcode, city and country (a user edit in between
/// wins) and that sets no user stamp; it and the state upsert share one transaction under the retrying execution
/// strategy, or the caller's transaction when one is open. State rows are written with INSERT ... ON CONFLICT so the backfill and a Klacksy call racing on the
/// same address never fail.
/// </summary>
/// <param name="context">Database context</param>

using Klacks.Api.Application.DTOs.Geocoding;
using Klacks.Api.Application.Interfaces.Geocoding;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Geocoding;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace Klacks.Api.Infrastructure.Repositories.Geocoding;

public class AddressGeocodingStateRepository : IAddressGeocodingStateRepository
{
    private const string ClearRequest = "requested_at = NULL, requested_by = NULL, requested_by_user_id = NULL";

    private const string AttemptColumns = "attempted_at, attempted_zip, attempted_city, attempted_country, outcome";

    private const string AttemptUpdate =
        "attempted_at = EXCLUDED.attempted_at, attempted_zip = EXCLUDED.attempted_zip, " +
        "attempted_city = EXCLUDED.attempted_city, attempted_country = EXCLUDED.attempted_country, outcome = EXCLUDED.outcome";

    private const string RecordAttemptSql =
        $"INSERT INTO {GeocodingTableNames.AddressGeocodingState} (address_id, {AttemptColumns}) " +
        "VALUES (@address_id, @attempted_at, @attempted_zip, @attempted_city, @attempted_country, @outcome) " +
        $"ON CONFLICT (address_id) DO UPDATE SET {AttemptUpdate}, {ClearRequest}";

    private const string RecordFillSql =
        $"INSERT INTO {GeocodingTableNames.AddressGeocodingState} (address_id, {AttemptColumns}, filled_at, fill_source, " +
        "filled_by, filled_by_user_id, filled_latitude, filled_longitude) " +
        "VALUES (@address_id, @attempted_at, @attempted_zip, @attempted_city, @attempted_country, @outcome, @attempted_at, " +
        "@fill_source, @filled_by, @filled_by_user_id, @filled_latitude, @filled_longitude) " +
        $"ON CONFLICT (address_id) DO UPDATE SET {AttemptUpdate}, {ClearRequest}, filled_at = EXCLUDED.filled_at, " +
        "fill_source = EXCLUDED.fill_source, filled_by = EXCLUDED.filled_by, filled_by_user_id = EXCLUDED.filled_by_user_id, " +
        "filled_latitude = EXCLUDED.filled_latitude, filled_longitude = EXCLUDED.filled_longitude";

    private const string RequestSql =
        $"INSERT INTO {GeocodingTableNames.AddressGeocodingState} (address_id, requested_at, requested_by, requested_by_user_id) " +
        "SELECT ids.address_id, @requested_at, @requested_by, @requested_by_user_id FROM unnest(@address_ids) AS ids(address_id) " +
        "ON CONFLICT (address_id) DO UPDATE SET requested_at = EXCLUDED.requested_at, requested_by = EXCLUDED.requested_by, " +
        "requested_by_user_id = EXCLUDED.requested_by_user_id";

    private readonly DataBaseContext _context;

    public AddressGeocodingStateRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<List<AddressGeocodingCandidate>> GetDueCandidatesAsync(
        AddressGeocodingDueQuery query, CancellationToken cancellationToken = default)
    {
        var rows = await (
                from row in MissingCoordinates()
                where row.Address.Zip.Trim() != string.Empty && row.Address.City.Trim() != string.Empty
                    && (query.IncludeEmployeeAddresses || row.Client.Type == EntityTypeEnum.Customer)
                    && (row.State == null
                        || row.State.RequestedAt != null
                        || row.State.AttemptedZip != row.Address.Zip
                        || row.State.AttemptedCity != row.Address.City
                        || row.State.AttemptedCountry != row.Address.Country
                        || (row.State.Outcome == AddressGeocodingOutcome.NotFound && row.State.AttemptedAt < query.NotFoundRetryBefore)
                        || (row.State.Outcome == AddressGeocodingOutcome.Unavailable && row.State.AttemptedAt < query.UnavailableRetryBefore))
                orderby row.State != null && row.State.RequestedAt != null descending, row.Address.Id
                select row)
            .Take(query.Limit)
            .ToListAsync(cancellationToken);

        return rows.Select(ToCandidate).ToList();
    }

    public async Task<List<AddressGeocodingCandidate>> GetMissingCoordinatesAsync(
        IReadOnlyCollection<Guid>? clientIds, CancellationToken cancellationToken = default)
    {
        var rows = MissingCoordinates();
        if (clientIds != null)
        {
            rows = rows.Where(row => clientIds.Contains(row.Client.Id));
        }

        var list = await rows.OrderBy(row => row.Client.Id).ThenBy(row => row.Address.Id).ToListAsync(cancellationToken);
        return list.Select(ToCandidate).ToList();
    }

    public async Task<List<AddressGeocodingCandidate>> GetMissingCoordinatesByIdsAsync(
        IReadOnlyCollection<Guid> addressIds, CancellationToken cancellationToken = default)
    {
        var list = await MissingCoordinates()
            .Where(row => addressIds.Contains(row.Address.Id))
            .OrderBy(row => row.Address.Id)
            .ToListAsync(cancellationToken);
        return list.Select(ToCandidate).ToList();
    }

    public async Task<bool> TryFillAsync(AddressGeocodingFill fill, CancellationToken cancellationToken = default)
    {
        if (_context.Database.CurrentTransaction != null)
        {
            return await WriteFillAsync(fill, cancellationToken);
        }

        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            if (!await WriteFillAsync(fill, cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            await transaction.CommitAsync(cancellationToken);
            return true;
        });
    }

    public async Task RecordAttemptAsync(
        AddressGeocodingCandidate candidate, AddressGeocodingOutcome outcome, DateTime at, CancellationToken cancellationToken = default)
    {
        await _context.Database.ExecuteSqlRawAsync(
            RecordAttemptSql,
            AttemptParameters(candidate.AddressId, candidate.Zip, candidate.City, candidate.Country, outcome, at),
            cancellationToken);
    }

    public async Task<int> RequestAsync(
        IReadOnlyCollection<Guid> addressIds, AddressGeocodingAttribution attribution, DateTime at, CancellationToken cancellationToken = default)
    {
        var ids = addressIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return 0;
        }

        return await _context.Database.ExecuteSqlRawAsync(
            RequestSql,
            [
                GeocodingSqlParameter.Create("address_ids", NpgsqlDbType.Array | NpgsqlDbType.Uuid, ids),
                GeocodingSqlParameter.Create("requested_at", NpgsqlDbType.TimestampTz, at),
                GeocodingSqlParameter.Create("requested_by", NpgsqlDbType.Integer, (int)attribution.Trigger),
                GeocodingSqlParameter.Create("requested_by_user_id", NpgsqlDbType.Uuid, attribution.UserId)
            ],
            cancellationToken);
    }

    private IQueryable<CandidateRow> MissingCoordinates() =>
        from address in _context.Address
        join client in _context.Client on address.ClientId equals client.Id
        join state in _context.AddressGeocodingStates on address.Id equals state.AddressId into states
        from state in states.DefaultIfEmpty()
        where !address.IsDeleted && address.Latitude == null && address.Longitude == null
        select new CandidateRow { Address = address, Client = client, State = state };

    private async Task<bool> WriteFillAsync(AddressGeocodingFill fill, CancellationToken cancellationToken)
    {
        if (await UpdateCoordinatesAsync(fill, cancellationToken) == 0)
        {
            return false;
        }

        await _context.Database.ExecuteSqlRawAsync(
            RecordFillSql,
            AttemptParameters(fill.AddressId, fill.ExpectedZip, fill.ExpectedCity, fill.ExpectedCountry, AddressGeocodingOutcome.Filled, fill.At)
                .Concat(
                [
                    GeocodingSqlParameter.Create("fill_source", NpgsqlDbType.Integer, (int)fill.Source),
                    GeocodingSqlParameter.Create("filled_by", NpgsqlDbType.Integer, (int)fill.Attribution.Trigger),
                    GeocodingSqlParameter.Create("filled_by_user_id", NpgsqlDbType.Uuid, fill.Attribution.UserId),
                    GeocodingSqlParameter.Create("filled_latitude", NpgsqlDbType.Double, fill.Latitude),
                    GeocodingSqlParameter.Create("filled_longitude", NpgsqlDbType.Double, fill.Longitude)
                ]),
            cancellationToken);
        return true;
    }

    private Task<int> UpdateCoordinatesAsync(AddressGeocodingFill fill, CancellationToken cancellationToken)
    {
        var target = _context.Address.Where(address => address.Id == fill.AddressId
            && !address.IsDeleted
            && address.Latitude == null
            && address.Longitude == null
            && address.Zip == fill.ExpectedZip
            && address.City == fill.ExpectedCity
            && address.Country == fill.ExpectedCountry);

        if (fill.StateIfEmpty == null)
        {
            return target.ExecuteUpdateAsync(setters => setters
                .SetProperty(address => address.Latitude, fill.Latitude)
                .SetProperty(address => address.Longitude, fill.Longitude), cancellationToken);
        }

        var state = fill.StateIfEmpty;
        return target.ExecuteUpdateAsync(setters => setters
            .SetProperty(address => address.Latitude, fill.Latitude)
            .SetProperty(address => address.Longitude, fill.Longitude)
            .SetProperty(address => address.State, address => address.State.Trim() == string.Empty ? state : address.State),
            cancellationToken);
    }

    private static NpgsqlParameter[] AttemptParameters(
        Guid addressId, string zip, string city, string country, AddressGeocodingOutcome outcome, DateTime at) =>
    [
        GeocodingSqlParameter.Create("address_id", NpgsqlDbType.Uuid, addressId),
        GeocodingSqlParameter.Create("attempted_at", NpgsqlDbType.TimestampTz, at),
        GeocodingSqlParameter.Create("attempted_zip", NpgsqlDbType.Text, zip),
        GeocodingSqlParameter.Create("attempted_city", NpgsqlDbType.Text, city),
        GeocodingSqlParameter.Create("attempted_country", NpgsqlDbType.Text, country),
        GeocodingSqlParameter.Create("outcome", NpgsqlDbType.Integer, (int)outcome)
    ];

    private static AddressGeocodingCandidate ToCandidate(CandidateRow row) => new(
        row.Address.Id,
        row.Client.Id,
        row.Client.Type,
        DisplayName(row.Client),
        row.Address.Zip,
        row.Address.City,
        row.Address.Country,
        row.Address.State,
        row.State?.RequestedAt != null && row.State.RequestedBy.HasValue
            ? new AddressGeocodingAttribution(row.State.RequestedBy.Value, row.State.RequestedByUserId)
            : null);

    private static string DisplayName(Client client)
    {
        var person = $"{client.FirstName} {client.Name}".Trim();
        return client.LegalEntity && !string.IsNullOrWhiteSpace(client.Company) ? client.Company! : person;
    }

    private sealed class CandidateRow
    {
        public Address Address { get; init; } = null!;

        public Client Client { get; init; } = null!;

        public AddressGeocodingState? State { get; init; }
    }
}
