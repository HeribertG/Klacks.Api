using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Domain.Services.Settings;

public class VatManagementService : IVatManagementService
{
    private readonly DataBaseContext _context;
    private readonly ILogger<VatManagementService> _logger;

    public VatManagementService(DataBaseContext context, ILogger<VatManagementService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Vat> AddVatAsync(Vat vat)
    {
        _logger.LogInformation("Adding new VAT with ID: {VatId}", vat.Id);
        await _context.Vat.AddAsync(vat);
        return vat;
    }

    public async Task<Vat> DeleteVatAsync(Guid id)
    {
        _logger.LogInformation("Deleting VAT with ID: {VatId}", id);
        var vat = await _context.Vat.FindAsync(id);
        
        if (vat == null)
        {
            _logger.LogWarning("VAT with ID: {VatId} not found for deletion", id);
            throw new InvalidOperationException($"VAT with ID {id} not found");
        }

        _context.Vat.Remove(vat);
        return vat;
    }

    public async Task<Vat> GetVatAsync(Guid id)
    {
        _logger.LogInformation("Retrieving VAT with ID: {VatId}", id);
        var vat = await _context.Vat.FindAsync(id);
        
        if (vat == null)
        {
            _logger.LogWarning("VAT with ID: {VatId} not found", id);
            throw new InvalidOperationException($"VAT with ID {id} not found");
        }

        return vat;
    }

    public async Task<List<Vat>> GetVatListAsync()
    {
        _logger.LogInformation("Retrieving all VATs");
        return await _context.Vat.ToListAsync();
    }

    public async Task<bool> VatExistsAsync(Guid id)
    {
        return await _context.Vat.AnyAsync(e => e.Id == id);
    }

    public async Task<Vat> UpdateVatAsync(Vat vat)
    {
        _logger.LogInformation("Updating VAT with ID: {VatId}", vat.Id);
        _context.Vat.Update(vat);
        return vat;
    }
}