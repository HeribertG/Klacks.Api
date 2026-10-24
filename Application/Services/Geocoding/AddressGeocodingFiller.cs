// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Geocodes one address to the centroid of its postcode and city (owner decision 2026-10-24: the automatic
/// backfill asks per locality; street-level geocoding stays where the address is entered). The persistent
/// locality cache answers first, so one postcode/city pair reaches Nominatim once; a cached "not found" is reused
/// until the not-found window ends. Only the postcode, city and country name are sent, never the street.
/// A hit is written through the conditional fill (coordinates only while still empty, state only when empty);
/// a miss or an unreachable geocoder is recorded as an attempt, an unreachable one is never cached. With geocoding
/// switched off by the operator nothing is read, asked or written. Postcode or city that are blank after
/// normalization (tabs, line breaks, non-breaking spaces) are recorded as not found, so the scan does not pick the
/// address again and again.
/// </summary>
/// <param name="geocodingService">Rate-limited Nominatim geocoder honouring the geo service policy</param>
/// <param name="localityRepository">Persistent per-locality cache</param>
/// <param name="stateRepository">Conditional coordinate write and attempt bookkeeping</param>
/// <param name="countryResolver">Resolves the stored country code; the default country stands in for an unknown one</param>
/// <param name="stateResolver">Maps the geocoder's state/county to the state abbreviation</param>
/// <param name="timeProvider">UTC clock of the attempt and fill timestamps</param>
/// <param name="logger">Unreachable geocoder and discarded results</param>

using Klacks.Api.Application.DTOs.Geocoding;
using Klacks.Api.Application.Interfaces.Geocoding;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.RouteOptimization;
using Klacks.Api.Domain.Models.Geocoding;
using Klacks.Api.Domain.Services.Common;
using Klacks.Api.Domain.Services.Geocoding;

namespace Klacks.Api.Application.Services.Geocoding;

public class AddressGeocodingFiller : IAddressGeocodingFiller
{
    private readonly IGeocodingService _geocodingService;
    private readonly IGeocodedLocalityRepository _localityRepository;
    private readonly IAddressGeocodingStateRepository _stateRepository;
    private readonly ICountryResolver _countryResolver;
    private readonly StateAbbreviationResolver _stateResolver;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AddressGeocodingFiller> _logger;

    public AddressGeocodingFiller(
        IGeocodingService geocodingService,
        IGeocodedLocalityRepository localityRepository,
        IAddressGeocodingStateRepository stateRepository,
        ICountryResolver countryResolver,
        StateAbbreviationResolver stateResolver,
        TimeProvider timeProvider,
        ILogger<AddressGeocodingFiller> logger)
    {
        _geocodingService = geocodingService;
        _localityRepository = localityRepository;
        _stateRepository = stateRepository;
        _countryResolver = countryResolver;
        _stateResolver = stateResolver;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<AddressGeocodingFillResult> FillAsync(
        AddressGeocodingCandidate candidate, AddressGeocodingAttribution attribution, CancellationToken cancellationToken = default)
    {
        if (!_geocodingService.IsEnabled)
        {
            return new AddressGeocodingFillResult(AddressGeocodingFillStatus.Disabled, false);
        }

        var country = await _countryResolver.ResolveAsync(candidate.Country, cancellationToken)
            ?? await _countryResolver.GetDefaultAsync(cancellationToken);
        var countryCode = country?.Abbreviation ?? candidate.Country;
        var key = GeocodingLocalityKey.Create(countryCode, candidate.Zip, candidate.City);
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        if (key == null)
        {
            await _stateRepository.RecordAttemptAsync(candidate, AddressGeocodingOutcome.NotFound, now, cancellationToken);
            return new AddressGeocodingFillResult(AddressGeocodingFillStatus.NotFound, false);
        }

        var cached = await _localityRepository.GetAsync(key, cancellationToken);
        if (cached != null && IsUsable(cached, now))
        {
            return await ApplyAsync(candidate, cached, countryCode, AddressGeocodingSource.LocalityCache, false, attribution, now, cancellationToken);
        }

        var result = await _geocodingService.ValidateExactAddressAsync(
            null, candidate.Zip, candidate.City, GeocodingCountryName.Of(country));
        if (result.Disabled)
        {
            return new AddressGeocodingFillResult(AddressGeocodingFillStatus.Disabled, true);
        }

        if (result.ServiceUnavailable)
        {
            _logger.LogWarning("Geocoding service unavailable for address {AddressId}; retried later", candidate.AddressId);
            await _stateRepository.RecordAttemptAsync(candidate, AddressGeocodingOutcome.Unavailable, now, cancellationToken);
            return new AddressGeocodingFillResult(AddressGeocodingFillStatus.Unavailable, true);
        }

        var locality = ToLocality(key, result, now);
        await _localityRepository.StoreAsync(locality, cancellationToken);
        return await ApplyAsync(candidate, locality, countryCode, AddressGeocodingSource.NominatimLocality, true, attribution, now, cancellationToken);
    }

    private static bool IsUsable(GeocodedLocality cached, DateTime now) =>
        cached.Found || cached.ResolvedAt > now.AddDays(-AddressGeocodingLimits.NotFoundRetryDays);

    private async Task<AddressGeocodingFillResult> ApplyAsync(
        AddressGeocodingCandidate candidate,
        GeocodedLocality locality,
        string countryCode,
        AddressGeocodingSource source,
        bool usedGeocoder,
        AddressGeocodingAttribution attribution,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (!locality.Found || !locality.Latitude.HasValue || !locality.Longitude.HasValue)
        {
            await _stateRepository.RecordAttemptAsync(candidate, AddressGeocodingOutcome.NotFound, now, cancellationToken);
            return new AddressGeocodingFillResult(AddressGeocodingFillStatus.NotFound, usedGeocoder);
        }

        var stateIfEmpty = string.IsNullOrWhiteSpace(candidate.State)
            ? await _stateResolver.ResolveAsync(countryCode, locality.State, locality.County)
            : null;

        var fill = new AddressGeocodingFill(
            candidate.AddressId, candidate.Zip, candidate.City, candidate.Country,
            locality.Latitude.Value, locality.Longitude.Value, stateIfEmpty, source, attribution, now);

        if (!await _stateRepository.TryFillAsync(fill, cancellationToken))
        {
            _logger.LogInformation("Address {AddressId} changed or got coordinates meanwhile; geocoding result discarded", candidate.AddressId);
            return new AddressGeocodingFillResult(AddressGeocodingFillStatus.Superseded, usedGeocoder);
        }

        return new AddressGeocodingFillResult(
            AddressGeocodingFillStatus.Filled, usedGeocoder, locality.Latitude, locality.Longitude, source);
    }

    private static GeocodedLocality ToLocality(GeocodingLocalityKey key, GeocodingValidationResult result, DateTime now)
    {
        var found = result.Found && result.Latitude.HasValue && result.Longitude.HasValue;
        return new GeocodedLocality
        {
            CountryCode = key.CountryCode,
            PostalCode = key.PostalCode,
            City = key.City,
            Found = found,
            Latitude = found ? result.Latitude : null,
            Longitude = found ? result.Longitude : null,
            State = found ? result.State : null,
            County = found ? result.County : null,
            ResolvedAt = now
        };
    }
}
