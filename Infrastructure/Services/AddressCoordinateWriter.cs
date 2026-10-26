// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Persists street-level coordinates the container autofill geocoded for an address. Like every automatic
/// geocoding write it only fills: the conditional write of IAddressGeocodingStateRepository lands only while the
/// address still has no coordinates and the postcode, city and country read here, and it records the fill
/// (source NominatimStreet, trigger Autofill) in the geocoding state.
/// </summary>
/// <param name="context">Reads the locality fields of the address</param>
/// <param name="stateRepository">Conditional coordinate write with audit</param>
/// <param name="timeProvider">UTC clock of the fill timestamp</param>

using Klacks.Api.Application.DTOs.Geocoding;
using Klacks.Api.Application.Interfaces.Geocoding;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.RouteOptimization;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services;

public class AddressCoordinateWriter : IAddressCoordinateWriter
{
    private static readonly AddressGeocodingAttribution AutofillAttribution = new(AddressGeocodingTrigger.Autofill, null);

    private readonly DataBaseContext _context;
    private readonly IAddressGeocodingStateRepository _stateRepository;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AddressCoordinateWriter> _logger;

    public AddressCoordinateWriter(
        DataBaseContext context,
        IAddressGeocodingStateRepository stateRepository,
        TimeProvider timeProvider,
        ILogger<AddressCoordinateWriter> logger)
    {
        _context = context;
        _stateRepository = stateRepository;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task UpdateCoordinatesAsync(Guid addressId, double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        var locality = await _context.Address
            .AsNoTracking()
            .Where(a => a.Id == addressId)
            .Select(a => new { a.Zip, a.City, a.Country })
            .FirstOrDefaultAsync(cancellationToken);
        if (locality == null)
        {
            return;
        }

        var filled = await _stateRepository.TryFillAsync(
            new AddressGeocodingFill(
                addressId, locality.Zip, locality.City, locality.Country, latitude, longitude, null,
                AddressGeocodingSource.NominatimStreet, AutofillAttribution, _timeProvider.GetUtcNow().UtcDateTime),
            cancellationToken);

        if (filled)
        {
            _logger.LogInformation("Saved coordinates for address {AddressId}", addressId);
        }
    }
}
