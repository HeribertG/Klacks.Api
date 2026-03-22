// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Persists geocoding coordinates in the address table via direct DB update.
/// </summary>
/// <param name="context">Database context for direct access to the address table</param>

using Klacks.Api.Domain.Interfaces.RouteOptimization;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services;

public class AddressCoordinateWriter : IAddressCoordinateWriter
{
    private readonly DataBaseContext _context;
    private readonly ILogger<AddressCoordinateWriter> _logger;

    public AddressCoordinateWriter(DataBaseContext context, ILogger<AddressCoordinateWriter> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task UpdateCoordinatesAsync(Guid addressId, double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        var updated = await _context.Address
            .Where(a => a.Id == addressId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(a => a.Latitude, latitude)
                .SetProperty(a => a.Longitude, longitude),
                cancellationToken);

        if (updated > 0)
        {
            _logger.LogInformation("Saved coordinates for address {AddressId}: {Lat}, {Lon}", addressId, latitude, longitude);
        }
    }
}
