using Klacks.Api.Application.Queries.Dashboard;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Presentation.DTOs.Dashboard;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Dashboard;

public class GetClientLocationsQueryHandler : IRequestHandler<GetClientLocationsQuery, IEnumerable<ClientLocationResource>>
{
    private readonly DataBaseContext _context;
    private readonly ILogger<GetClientLocationsQueryHandler> _logger;

    public GetClientLocationsQueryHandler(
        DataBaseContext context,
        ILogger<GetClientLocationsQueryHandler> logger)
    {
        _context = context;
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

            var result = clients.Select(client =>
            {
                var currentAddress = client.Addresses
                    .Where(a => a.ValidFrom.HasValue && a.ValidFrom.Value <= DateTime.Now)
                    .OrderByDescending(a => a.ValidFrom)
                    .FirstOrDefault();

                return new ClientLocationResource
                {
                    Id = client.Id.ToString(),
                    Type = (int)client.Type,
                    CurrentAddress = currentAddress != null ? new AddressInfo
                    {
                        City = currentAddress.City ?? string.Empty,
                        Country = currentAddress.Country ?? string.Empty,
                        Zip = currentAddress.Zip ?? string.Empty
                    } : null
                };
            }).Where(c => c.CurrentAddress != null);

            _logger.LogInformation($"Retrieved {result.Count()} client locations");

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching client locations");
            throw;
        }
    }
}
