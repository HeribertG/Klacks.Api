using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Dashboard;
using Klacks.Api.Infrastructure.Services;
using Klacks.Api.Presentation.DTOs.Dashboard;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Dashboard;

public class GetClientLocationsQueryHandler : IRequestHandler<GetClientLocationsQuery, IEnumerable<ClientLocationResource>>
{
    private readonly IClientRepository _clientRepository;
    private readonly IGeocodingService _geocodingService;
    private readonly ILogger<GetClientLocationsQueryHandler> _logger;

    public GetClientLocationsQueryHandler(
        IClientRepository clientRepository,
        IGeocodingService geocodingService,
        ILogger<GetClientLocationsQueryHandler> logger)
    {
        _clientRepository = clientRepository;
        _geocodingService = geocodingService;
        _logger = logger;
    }

    public async Task<IEnumerable<ClientLocationResource>> Handle(GetClientLocationsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching client locations for dashboard");

        try
        {
            var clients = await _clientRepository.GetActiveClientsWithAddressesAsync(cancellationToken);

            var clientsWithAddresses = clients
                .Select(client =>
                {
                    var currentAddress = client.Addresses
                        .Where(a => a.ValidFrom.HasValue && a.ValidFrom.Value <= DateTime.Now)
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
                var (latitude, longitude) = await _geocodingService.GeocodeAsync(
                    address.City ?? string.Empty,
                    address.Country ?? string.Empty
                );

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

            _logger.LogInformation($"Retrieved {result.Count} client locations");

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching client locations");
            throw;
        }
    }
}
