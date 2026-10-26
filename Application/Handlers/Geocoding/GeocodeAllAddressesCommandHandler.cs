// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Admin batch (POST RouteOptimization/geocode-all): geocodes now every address without coordinates that the
/// geocoding rules allow, through the same system function as Klacksy and the background backfill - locality
/// centroid via the persistent cache, only empty coordinates filled, employee addresses only when the installation
/// allows it, attributed to the admin. Total counts the eligible addresses, Geocoded the filled ones and Failed the
/// rest (not found, changed meanwhile, or handed to the backfill after the geocoder became unreachable). A disabled
/// geocoder is reported instead of attempted. The background backfill fills the same addresses on its own; this
/// call only does it at once.
/// </summary>
/// <param name="stateRepository">Lists the addresses without coordinates</param>
/// <param name="geocodingService">Selects eligible addresses and geocodes them</param>
/// <param name="logger">Batch summary</param>

using Klacks.Api.Application.Commands.Geocoding;
using Klacks.Api.Application.DTOs.Geocoding;
using Klacks.Api.Application.Interfaces.Geocoding;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Geocoding;

public class GeocodeAllAddressesCommandHandler : IRequestHandler<GeocodeAllAddressesCommand, GeocodeAllAddressesResult>
{
    private readonly IAddressGeocodingStateRepository _stateRepository;
    private readonly IAddressGeocodingService _geocodingService;
    private readonly ILogger<GeocodeAllAddressesCommandHandler> _logger;

    public GeocodeAllAddressesCommandHandler(
        IAddressGeocodingStateRepository stateRepository,
        IAddressGeocodingService geocodingService,
        ILogger<GeocodeAllAddressesCommandHandler> logger)
    {
        _stateRepository = stateRepository;
        _geocodingService = geocodingService;
        _logger = logger;
    }

    public async Task<GeocodeAllAddressesResult> Handle(GeocodeAllAddressesCommand request, CancellationToken cancellationToken)
    {
        if (!_geocodingService.IsGeocodingEnabled)
        {
            return GeocodeAllAddressesResult.Disabled;
        }

        var missing = await _stateRepository.GetMissingCoordinatesAsync(null, cancellationToken);
        var selection = await _geocodingService.SelectAsync(missing, cancellationToken);
        var summary = await _geocodingService.GeocodeAsync(
            selection.Eligible,
            new AddressGeocodingAttribution(AddressGeocodingTrigger.AdminBatch, null),
            AddressGeocodingBudget.Unlimited,
            cancellationToken);

        if (summary.GeocodingDisabled)
        {
            return GeocodeAllAddressesResult.Disabled;
        }

        var total = selection.Eligible.Count;
        var geocoded = summary.Filled.Count;
        _logger.LogInformation(
            "Batch geocoding: {Geocoded} of {Total} eligible address(es) filled, {Queued} handed to the backfill, " +
            "{PrivacyBlocked} employee address(es) skipped by the privacy setting, {Incomplete} without postcode or city",
            geocoded, total, summary.Queued, selection.PrivacyBlocked.Count, selection.Incomplete.Count);

        return new GeocodeAllAddressesResult(false, total, geocoded, total - geocoded);
    }
}
