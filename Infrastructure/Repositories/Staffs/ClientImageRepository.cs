using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Staffs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Infrastructure.Repositories.Staffs;

public class ClientImageRepository : IClientImageRepository
{
    private readonly DataBaseContext _context;
    private readonly ILogger<ClientImageRepository> _logger;

    public ClientImageRepository(DataBaseContext context, ILogger<ClientImageRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ClientImage?> GetByClientIdAsync(Guid clientId)
    {
        _logger.LogInformation("Fetching client image for ClientId: {ClientId}", clientId);
        return await _context.ClientImage
            .AsTracking()
            .FirstOrDefaultAsync(ci => ci.ClientId == clientId);
    }

    public async Task<bool> DeleteByClientIdAsync(Guid clientId)
    {
        _logger.LogInformation("Deleting client image for ClientId: {ClientId}", clientId);
        var image = await GetByClientIdAsync(clientId);
        if (image == null)
        {
            _logger.LogWarning("No image found for ClientId: {ClientId}", clientId);
            return false;
        }

        _context.ClientImage.Remove(image);
        _logger.LogInformation("Client image marked for deletion for ClientId: {ClientId}", clientId);
        return true;
    }

    public async Task Add(ClientImage clientImage)
    {
        await _context.ClientImage.AddAsync(clientImage);
    }

    public async Task Delete(Guid id)
    {
        var image = await _context.ClientImage.FindAsync(id);
        if (image != null)
        {
            _context.ClientImage.Remove(image);
        }
    }
}
