// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// One pass of the background address geocoding. It reads one batch of due addresses (requested first; customers
/// always, employees only when the installation allows it, read fresh per pass) and geocodes them one at a time,
/// attributing each fill to the pending request or to the system scan. After every real Nominatim request it
/// pauses, so interactive address checks and searches still get the shared, rate-limited slot; cache hits need no
/// pause. An unreachable or switched-off geocoder ends the pass early: the rest stays due for the next interval
/// instead of meeting the same outage one by one.
/// </summary>
/// <param name="stateRepository">Reads the due candidates</param>
/// <param name="filler">Geocodes and fills one address</param>
/// <param name="settings">Employee-address privacy switch</param>
/// <param name="geocodingService">Tells whether geocoding is enabled</param>
/// <param name="timeProvider">UTC clock of the retry windows and the pause timer</param>

using Klacks.Api.Application.DTOs.Geocoding;
using Klacks.Api.Application.Interfaces.Geocoding;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.RouteOptimization;

namespace Klacks.Api.Application.Services.Geocoding;

public class AddressGeocodingBackfillRunner : IAddressGeocodingBackfillRunner
{
    private readonly IAddressGeocodingStateRepository _stateRepository;
    private readonly IAddressGeocodingFiller _filler;
    private readonly IAddressGeocodingSettings _settings;
    private readonly IGeocodingService _geocodingService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AddressGeocodingBackfillRunner> _logger;
    private readonly Func<CancellationToken, Task> _pauseAfterGeocoderCall;

    public AddressGeocodingBackfillRunner(
        IAddressGeocodingStateRepository stateRepository,
        IAddressGeocodingFiller filler,
        IAddressGeocodingSettings settings,
        IGeocodingService geocodingService,
        TimeProvider timeProvider,
        ILogger<AddressGeocodingBackfillRunner> logger)
        : this(stateRepository, filler, settings, geocodingService, timeProvider, logger,
            token => Task.Delay(TimeSpan.FromMilliseconds(AddressGeocodingLimits.PauseAfterGeocoderCallMilliseconds), timeProvider, token))
    {
    }

    internal AddressGeocodingBackfillRunner(
        IAddressGeocodingStateRepository stateRepository,
        IAddressGeocodingFiller filler,
        IAddressGeocodingSettings settings,
        IGeocodingService geocodingService,
        TimeProvider timeProvider,
        ILogger<AddressGeocodingBackfillRunner> logger,
        Func<CancellationToken, Task> pauseAfterGeocoderCall)
    {
        _stateRepository = stateRepository;
        _filler = filler;
        _settings = settings;
        _geocodingService = geocodingService;
        _timeProvider = timeProvider;
        _logger = logger;
        _pauseAfterGeocoderCall = pauseAfterGeocoderCall;
    }

    public async Task<bool> RunBatchAsync(CancellationToken cancellationToken)
    {
        if (!_geocodingService.IsEnabled)
        {
            return false;
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var query = new AddressGeocodingDueQuery(
            await _settings.AreEmployeeAddressesAllowedAsync(cancellationToken),
            now.AddDays(-AddressGeocodingLimits.NotFoundRetryDays),
            now.AddMinutes(-AddressGeocodingLimits.UnavailableRetryMinutes),
            AddressGeocodingLimits.ScanBatchSize);
        var candidates = await _stateRepository.GetDueCandidatesAsync(query, cancellationToken);

        var filled = 0;
        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await _filler.FillAsync(
                candidate, candidate.PendingRequest ?? AddressGeocodingAttribution.SystemScan, cancellationToken);
            filled += result.Status == AddressGeocodingFillStatus.Filled ? 1 : 0;

            if (result.UsedGeocoder)
            {
                await _pauseAfterGeocoderCall(cancellationToken);
            }

            if (result.Status is AddressGeocodingFillStatus.Unavailable or AddressGeocodingFillStatus.Disabled)
            {
                _logger.LogWarning("Address geocoding pass stopped early: geocoder {Status}", result.Status);
                return false;
            }
        }

        if (candidates.Count > 0)
        {
            _logger.LogInformation("Address geocoding pass: {Filled} of {Count} address(es) filled", filled, candidates.Count);
        }

        return candidates.Count >= AddressGeocodingLimits.ScanBatchSize;
    }
}
