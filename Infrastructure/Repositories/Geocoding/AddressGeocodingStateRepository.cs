// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads geocoding candidates and writes coordinates, attempts and requests (self-committing, see
/// IAddressGeocodingStateRepository). Candidates are projected to the few fields the geocoding needs (no entity
/// loading). Only addresses with neither latitude nor longitude are candidates: one with a single coordinate is left
/// as it is, because the fill never overwrites. An address whose earlier fill was lost (coordinates empty again,
/// e.g. a stale client save) is due again. The coordinate write is one ExecuteUpdate
/// whose WHERE pins id, empty coordinates and the geocoded postcode, city and country (a user edit in between
/// wins) and that sets no user stamp; it and the state upsert share one transaction under the retrying execution
/// strategy, or the caller's transaction when one is open. State rows are written with INSERT ... ON CONFLICT so the backfill and a Klacksy call racing on the
/// same address never fail.
/// </summary>
/// <param name="context">Database context</param>

using Klacks.Api.Application.DTOs.Geocoding;
using Klacks.Api.Application.Interfaces.Geocoding;
using Klacks.Api.Domain.Enums;
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
                where row.Zip.Trim() != string.Empty && row.City.Trim() != string.Empty
                    && (query.IncludeEmployeeAddresses || row.ClientType == EntityTypeEnum.Customer)
                    && (row.AttemptedAt == null
                        || row.RequestedAt != null
                        || row.Outcome == AddressGeocodingOutcome.Filled
                        || row.AttemptedZip != row.Zip
                        || row.AttemptedCity != row.City
                        || row.AttemptedCountry != row.Country
                        || (row.Outcome == AddressGeocodingOutcome.NotFound && row.AttemptedAt < query.NotFoundRetryBefore)
                        || (row.Outcome == AddressGeocodingOutcome.Unavailable && row.AttemptedAt < query.UnavailableRetryBefore))
                orderby row.RequestedAt != null descending, row.AddressId
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
            rows = rows.Where(row => clientIds.Contains(row.ClientId));
        }

        var list = await rows.OrderBy(row => row.ClientId).ThenBy(row => row.AddressId).ToListAsync(cancellationToken);
        return list.Select(ToCandidate).ToList();
    }

    public async Task<List<AddressGeocodingCandidate>> GetMissingCoordinatesByIdsAsync(
        IReadOnlyCollection<Guid> addressIds, CancellationToken cancellationToken = default)
    {
        var list = await MissingCoordinates()
            .Where(row => addressIds.Contains(row.AddressId))
            .OrderBy(row => row.AddressId)
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
        from address in _context.Address.AsNoTracking()
        join client in _context.Client.AsNoTracking() on address.ClientId equals client.Id
        join state in _context.AddressGeocodingStates.AsNoTracking() on address.Id equals state.AddressId into states
        from state in states.DefaultIfEmpty()
        where !address.IsDeleted && address.Latitude == null && address.Longitude == null
        select new CandidateRow
        {
            AddressId = address.Id,
            ClientId = client.Id,
            ClientType = client.Type,
            FirstName = client.FirstName,
            Name = client.Name,
            Company = client.Company,
            LegalEntity = client.LegalEntity,
            Zip = address.Zip,
            City = address.City,
            Country = address.Country,
            State = address.State,
            AttemptedAt = state == null ? null : state.AttemptedAt,
            AttemptedZip = state == null ? null : state.AttemptedZip,
            AttemptedCity = state == null ? null : state.AttemptedCity,
            AttemptedCountry = state == null ? null : state.AttemptedCountry,
            Outcome = state == null ? null : state.Outcome,
            RequestedAt = state == null ? null : state.RequestedAt,
            RequestedBy = state == null ? null : state.RequestedBy,
            RequestedByUserId = state == null ? null : state.RequestedByUserId
        };

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
        row.AddressId,
        row.ClientId,
        row.ClientType,
        DisplayName(row),
        row.Zip,
        row.City,
        row.Country,
        row.State,
        row.RequestedAt != null && row.RequestedBy.HasValue
            ? new AddressGeocodingAttribution(row.RequestedBy.Value, row.RequestedByUserId)
            : null);

    private static string DisplayName(CandidateRow row)
    {
        var person = $"{row.FirstName} {row.Name}".Trim();
        return row.LegalEntity && !string.IsNullOrWhiteSpace(row.Company) ? row.Company! : person;
    }

    private sealed class CandidateRow
    {
        public Guid AddressId { get; init; }

        public Guid ClientId { get; init; }

        public EntityTypeEnum ClientType { get; init; }

        public string? FirstName { get; init; }

        public string Name { get; init; } = string.Empty;

        public string? Company { get; init; }

        public bool LegalEntity { get; init; }

        public string Zip { get; init; } = string.Empty;

        public string City { get; init; } = string.Empty;

        public string Country { get; init; } = string.Empty;

        public string State { get; init; } = string.Empty;

        public DateTime? AttemptedAt { get; init; }

        public string? AttemptedZip { get; init; }

        public string? AttemptedCity { get; init; }

        public string? AttemptedCountry { get; init; }

        public AddressGeocodingOutcome? Outcome { get; init; }

        public DateTime? RequestedAt { get; init; }

        public AddressGeocodingTrigger? RequestedBy { get; init; }

        public Guid? RequestedByUserId { get; init; }
    }
}
