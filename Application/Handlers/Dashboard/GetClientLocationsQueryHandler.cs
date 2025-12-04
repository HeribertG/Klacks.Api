using Klacks.Api.Application.Queries.Dashboard;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Infrastructure.Services;
using Klacks.Api.Presentation.DTOs.Dashboard;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Dashboard;

public class GetClientLocationsQueryHandler : IRequestHandler<GetClientLocationsQuery, IEnumerable<ClientLocationResource>>
{
    private readonly DataBaseContext _context;
    private readonly IGeocodingService _geocodingService;
    private readonly ILogger<GetClientLocationsQueryHandler> _logger;

    public GetClientLocationsQueryHandler(
        DataBaseContext context,
        IGeocodingService geocodingService,
        ILogger<GetClientLocationsQueryHandler> logger)
    {
        _context = context;
        _geocodingService = geocodingService;
        _logger = logger;
    }

    public async Task<IEnumerable<ClientLocationResource>> Handle(GetClientLocationsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching client locations for dashboard");

        try
        {
            var clients = await _context.Client
                .Include(c => c.Addresses)
                .Where(c => !c.IsDeleted)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

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
