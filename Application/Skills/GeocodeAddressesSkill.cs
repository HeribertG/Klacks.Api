// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Gives addresses without coordinates the position of their postcode and city - for one named client, or for every
/// client the user may see. Owner decision 2026-10-24: Klacksy stores these coordinates WITHOUT asking (risk class
/// FillEmptyOnly), but narrowly: only empty coordinates are filled, never overwritten, through the system function
/// IAddressGeocodingService (no user edit, so no edit right is needed; whoever may see the address may trigger it,
/// hidden clients are filtered out), employee addresses only when the installation allows it, and the answer names
/// what was written ("without asking, but not without telling"). Only a few geocoder requests run inside the chat
/// turn; the rest is handed to the background geocoding, which survives restarts.
/// </summary>
/// <param name="clientId">Optional. UUID of the client whose addresses to locate.</param>
/// <param name="firstName">Optional. First name of the client.</param>
/// <param name="lastName">Optional. Last name or company name of the client. Without any client every visible client is covered.</param>
/// <param name="idNumber">Optional. Visible client number that disambiguates duplicate names.</param>

using System.Globalization;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Geocoding;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Geocoding;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillName)]
public class GeocodeAddressesSkill : BaseSkillImplementation
{
    public const string SkillName = "geocode_addresses";

    private readonly IClientSearchRepository _searchRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IClientVisibilityGuard _visibilityGuard;
    private readonly IAddressGeocodingStateRepository _stateRepository;
    private readonly IAddressGeocodingService _geocodingService;

    public GeocodeAddressesSkill(
        IClientSearchRepository searchRepository,
        IClientRepository clientRepository,
        IClientVisibilityGuard visibilityGuard,
        IAddressGeocodingStateRepository stateRepository,
        IAddressGeocodingService geocodingService)
    {
        _searchRepository = searchRepository;
        _clientRepository = clientRepository;
        _visibilityGuard = visibilityGuard;
        _stateRepository = stateRepository;
        _geocodingService = geocodingService;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        if (!_geocodingService.IsGeocodingEnabled)
        {
            return SkillResult.Error(GeoServiceMessages.GeocodingDisabled);
        }

        var (clientIds, error) = await ResolveScopeAsync(parameters, cancellationToken);
        if (error != null)
        {
            return SkillResult.Error(error);
        }

        var missing = await _stateRepository.GetMissingCoordinatesAsync(clientIds, cancellationToken);
        var visible = await _visibilityGuard.FilterVisibleAsync(missing, candidate => candidate.ClientId, cancellationToken);
        var selection = await _geocodingService.SelectAsync(visible, cancellationToken);
        var summary = await _geocodingService.GeocodeAsync(
            selection.Eligible,
            new AddressGeocodingAttribution(AddressGeocodingTrigger.Assistant, context.UserId),
            new AddressGeocodingBudget(
                AddressGeocodingLimits.AssistantGeocoderCallBudget, AddressGeocodingLimits.AssistantFillLimit, true),
            cancellationToken);

        if (summary.GeocodingDisabled)
        {
            return SkillResult.Error(GeoServiceMessages.GeocodingDisabled);
        }

        return SkillResult.SuccessResult(BuildData(selection, summary), GeocodeAddressesMessage.Build(selection, summary));
    }

    private async Task<(IReadOnlyCollection<Guid>? ClientIds, string? Error)> ResolveScopeAsync(
        Dictionary<string, object> parameters, CancellationToken cancellationToken)
    {
        var clientId = GetParameter<string>(parameters, "clientId");
        var firstName = GetParameter<string>(parameters, "firstName");
        var lastName = GetParameter<string>(parameters, "lastName");
        if (string.IsNullOrWhiteSpace(clientId) && string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName))
        {
            return (null, null);
        }

        var (client, error) = await ClientResolver.ResolveByIdOrNameAsync(
            _searchRepository, _clientRepository, _visibilityGuard, clientId, firstName, lastName,
            GetParameter<int?>(parameters, ClientResolver.IdNumberParameterName), cancellationToken);
        return client == null ? (null, error) : ([client.Id], null);
    }

    private static object BuildData(AddressGeocodingSelection selection, AddressGeocodingRunSummary summary) => new
    {
        FilledCount = summary.Filled.Count,
        QueuedCount = summary.Queued,
        DeferredCount = summary.Deferred,
        NotFoundCount = summary.NotFound,
        PrivacySkippedCount = selection.PrivacyBlocked.Count,
        IncompleteCount = selection.Incomplete.Count,
        Filled = summary.Filled
            .Take(AddressGeocodingLimits.AssistantReportedAddressLimit)
            .Select(filled => new
            {
                filled.Address.ClientName,
                filled.Address.Zip,
                filled.Address.City,
                Latitude = filled.Latitude.ToString(GeocodeAddressesMessage.CoordinateFormat, CultureInfo.InvariantCulture),
                Longitude = filled.Longitude.ToString(GeocodeAddressesMessage.CoordinateFormat, CultureInfo.InvariantCulture),
                Source = filled.Source.ToString()
            })
            .ToList()
    };
}
