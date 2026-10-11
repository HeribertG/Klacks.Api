// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Lists the visible active clients with their current address for the dashboard map. The marker position is the
/// city-level geocode (only city and country leave the installation); when geocoding is switched off or finds
/// nothing, the stored address coordinates are used instead, otherwise none.
/// </summary>
/// <param name="geocodingService">City-level geocoder, skipped entirely when disabled</param>
/// <param name="groupVisibilityService">Restricts the clients to the caller's visible groups</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Dashboard;
using Klacks.Api.Domain.Interfaces.RouteOptimization;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Application.DTOs.Dashboard;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Dashboard;

public class GetClientLocationsQueryHandler : IRequestHandler<GetClientLocationsQuery, IEnumerable<ClientLocationResource>>
{
    private readonly IClientRepository _clientRepository;
    private readonly IGeocodingService _geocodingService;
    private readonly IGroupVisibilityService _groupVisibilityService;
    private readonly ILogger<GetClientLocationsQueryHandler> _logger;

    public GetClientLocationsQueryHandler(
        IClientRepository clientRepository,
        IGeocodingService geocodingService,
        IGroupVisibilityService groupVisibilityService,
        ILogger<GetClientLocationsQueryHandler> logger)
    {
        _clientRepository = clientRepository;
        _geocodingService = geocodingService;
        _groupVisibilityService = groupVisibilityService;
        _logger = logger;
    }

    public async Task<IEnumerable<ClientLocationResource>> Handle(GetClientLocationsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching client locations for dashboard");

        try
        {
            var scope = await _groupVisibilityService.GetVisibilityScopeAsync();
            List<Domain.Models.Staffs.Client> clients;

            if (scope.IsUnrestricted)
            {
                clients = await _clientRepository.GetActiveClientsWithAddressesAsync(cancellationToken);
            }
            else
            {
                if (scope.VisibleRootIds.Count == 0)
                {
                    _logger.LogWarning("Non-admin user has no visible groups for dashboard locations");
                    return [];
                }
                clients = await _clientRepository.GetActiveClientsWithAddressesForGroupsAsync(scope.VisibleRootIds.ToList(), cancellationToken);
            }

            var clientsWithAddresses = clients
                .Select(client =>
                {
                    var currentAddress = client.Addresses
                        .Where(a => a.ValidFrom.HasValue && a.ValidFrom.Value <= DateTime.UtcNow)
                        .OrderByDescending(a => a.ValidFrom)
                        .FirstOrDefault();

                    return new
                    {
                        Client = client,
                        Address = currentAddress
                    };
                })
                .Where(x => x.Address != null)
                .ToList();

            var result = new List<ClientLocationResource>();

            foreach (var item in clientsWithAddresses)
            {
                var address = item.Address!;
                var (latitude, longitude) = await ResolveCityCoordinatesAsync(address);

                result.Add(new ClientLocationResource
                {
                    Id = item.Client.Id.ToString(),
                    Type = (int)item.Client.Type,
                    CurrentAddress = new AddressInfo
                    {
                        City = address.City ?? string.Empty,
                        Country = address.Country ?? string.Empty,
                        Zip = address.Zip ?? string.Empty,
                        Latitude = latitude,
                        Longitude = longitude
                    }
                });
            }

            _logger.LogInformation("Retrieved {Count} client locations", result.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching client locations");
            throw;
        }
    }

    private async Task<(double? Latitude, double? Longitude)> ResolveCityCoordinatesAsync(Domain.Models.Staffs.Address address)
    {
        if (_geocodingService.IsEnabled)
        {
            var (latitude, longitude) = await _geocodingService.GeocodeAsync(
                address.City ?? string.Empty,
                address.Country ?? string.Empty);

            if (latitude.HasValue && longitude.HasValue)
            {
                return (latitude, longitude);
            }
        }

        return address.Latitude.HasValue && address.Longitude.HasValue
            ? (address.Latitude, address.Longitude)
            : (null, null);
    }
}
