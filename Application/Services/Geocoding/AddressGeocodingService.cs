// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// System function that gives addresses coordinates for a trigger (Klacksy for a user, the import, the admin
/// batch). It writes as the system, not as a user edit: only empty coordinates are filled (AddressGeocodingFiller,
/// conditional write), employee addresses only when the installation allows it (AddressGeocodingPrivacy), and what
/// is not done synchronously is requested for the background backfill, which the persisted request survives a
/// restart. A run or request without a user (import, admin batch) is attributed to the signed-in user of the request.
/// Visibility is not checked here: callers pass only addresses the acting user may see. GeocodeAsync applies the
/// privacy rule again itself (fresh setting), so a caller that skipped SelectAsync cannot send an employee address,
/// and a failing hand-over to the backfill never hides what was already written (the rest counts as deferred).
/// </summary>
/// <param name="filler">Geocodes and fills one address</param>
/// <param name="stateRepository">Reads candidates by id and writes requests</param>
/// <param name="settings">Employee-address privacy switch</param>
/// <param name="geocodingService">Tells whether geocoding is enabled at all</param>
/// <param name="availability">Whether this instance drains requests</param>
/// <param name="httpContextAccessor">Signed-in user for requests that name none</param>
/// <param name="timeProvider">UTC clock of the request timestamps</param>
/// <param name="logger">Failed hand-over to the backfill (the run itself is still reported)</param>

using System.Security.Claims;
using Klacks.Api.Application.DTOs.Geocoding;
using Klacks.Api.Application.Interfaces.Geocoding;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.RouteOptimization;
using Klacks.Api.Domain.Services.Geocoding;

namespace Klacks.Api.Application.Services.Geocoding;

public class AddressGeocodingService : IAddressGeocodingService
{
    private readonly IAddressGeocodingFiller _filler;
    private readonly IAddressGeocodingStateRepository _stateRepository;
    private readonly IAddressGeocodingSettings _settings;
    private readonly IGeocodingService _geocodingService;
    private readonly AddressGeocodingBackfillAvailability _availability;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AddressGeocodingService> _logger;

    public AddressGeocodingService(
        IAddressGeocodingFiller filler,
        IAddressGeocodingStateRepository stateRepository,
        IAddressGeocodingSettings settings,
        IGeocodingService geocodingService,
        AddressGeocodingBackfillAvailability availability,
        IHttpContextAccessor httpContextAccessor,
        TimeProvider timeProvider,
        ILogger<AddressGeocodingService> logger)
    {
        _filler = filler;
        _stateRepository = stateRepository;
        _settings = settings;
        _geocodingService = geocodingService;
        _availability = availability;
        _httpContextAccessor = httpContextAccessor;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public bool IsGeocodingEnabled => _geocodingService.IsEnabled;

    public async Task<AddressGeocodingSelection> SelectAsync(
        IReadOnlyList<AddressGeocodingCandidate> candidates, CancellationToken cancellationToken = default)
    {
        var employeeAddressesAllowed = await _settings.AreEmployeeAddressesAllowedAsync(cancellationToken);
        var eligible = new List<AddressGeocodingCandidate>();
        var privacyBlocked = new List<AddressGeocodingCandidate>();
        var incomplete = new List<AddressGeocodingCandidate>();

        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate.Zip) || string.IsNullOrWhiteSpace(candidate.City))
            {
                incomplete.Add(candidate);
            }
            else if (!AddressGeocodingPrivacy.MayGeocode(candidate.ClientType, employeeAddressesAllowed))
            {
                privacyBlocked.Add(candidate);
            }
            else
            {
                eligible.Add(candidate);
            }
        }

        return new AddressGeocodingSelection(eligible, privacyBlocked, incomplete);
    }

    public async Task<AddressGeocodingRunSummary> GeocodeAsync(
        IReadOnlyList<AddressGeocodingCandidate> eligible,
        AddressGeocodingAttribution attribution,
        AddressGeocodingBudget budget,
        CancellationToken cancellationToken = default)
    {
        if (!_geocodingService.IsEnabled)
        {
            return AddressGeocodingRunSummary.Disabled;
        }

        attribution = WithSignedInUser(attribution);
        var employeeAddressesAllowed = await _settings.AreEmployeeAddressesAllowedAsync(cancellationToken);
        var filled = new List<AddressGeocodingFilledAddress>();
        var remainder = new List<AddressGeocodingCandidate>();
        int notFound = 0, unavailable = 0, superseded = 0, processed = 0, geocoderCalls = 0;
        var stopped = false;

        foreach (var candidate in eligible.Where(c => AddressGeocodingPrivacy.MayGeocode(c.ClientType, employeeAddressesAllowed)))
        {
            if (stopped || IsExhausted(budget, processed, geocoderCalls))
            {
                remainder.Add(candidate);
                continue;
            }

            var result = await _filler.FillAsync(candidate, attribution, cancellationToken);
            processed++;
            geocoderCalls += result.UsedGeocoder ? 1 : 0;

            switch (result.Status)
            {
                case AddressGeocodingFillStatus.Filled:
                    filled.Add(new AddressGeocodingFilledAddress(
                        candidate, result.Latitude!.Value, result.Longitude!.Value, result.Source!.Value));
                    break;
                case AddressGeocodingFillStatus.NotFound:
                    notFound++;
                    break;
                case AddressGeocodingFillStatus.Superseded:
                    superseded++;
                    break;
                case AddressGeocodingFillStatus.Disabled:
                    return AddressGeocodingRunSummary.Disabled;
                default:
                    unavailable++;
                    remainder.Add(candidate);
                    stopped = true;
                    break;
            }
        }

        var queued = budget.QueueRemainder ? await TryRequestRemainderAsync(remainder, attribution, cancellationToken) : 0;

        return new AddressGeocodingRunSummary(false, filled, notFound, unavailable, superseded, queued, remainder.Count - queued);
    }

    public async Task<int> RequestAsync(
        IReadOnlyCollection<Guid> addressIds, AddressGeocodingAttribution attribution, CancellationToken cancellationToken = default)
    {
        if (addressIds.Count == 0 || !_geocodingService.IsEnabled || !_availability.IsActive)
        {
            return 0;
        }

        var candidates = await _stateRepository.GetMissingCoordinatesByIdsAsync(addressIds, cancellationToken);
        var selection = await SelectAsync(candidates, cancellationToken);
        return await RequestEligibleAsync(selection.Eligible.Select(c => c.AddressId).ToList(), attribution, cancellationToken);
    }

    private async Task<int> TryRequestRemainderAsync(
        IReadOnlyList<AddressGeocodingCandidate> remainder, AddressGeocodingAttribution attribution, CancellationToken cancellationToken)
    {
        try
        {
            return await RequestEligibleAsync(remainder.Select(c => c.AddressId).ToList(), attribution, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Requesting {Count} address(es) for the background geocoding failed; reported as deferred", remainder.Count);
            return 0;
        }
    }

    private static bool IsExhausted(AddressGeocodingBudget budget, int processed, int geocoderCalls) =>
        processed >= (budget.MaxFills ?? int.MaxValue) || geocoderCalls >= (budget.MaxGeocoderCalls ?? int.MaxValue);

    private async Task<int> RequestEligibleAsync(
        IReadOnlyCollection<Guid> addressIds, AddressGeocodingAttribution attribution, CancellationToken cancellationToken)
    {
        if (addressIds.Count == 0 || !_availability.IsActive)
        {
            return 0;
        }

        return await _stateRepository.RequestAsync(
            addressIds, WithSignedInUser(attribution), _timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
    }

    private AddressGeocodingAttribution WithSignedInUser(AddressGeocodingAttribution attribution) =>
        attribution.UserId.HasValue ? attribution : attribution with { UserId = CurrentUserId() };

    private Guid? CurrentUserId()
    {
        var claim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var userId) ? userId : null;
    }
}
