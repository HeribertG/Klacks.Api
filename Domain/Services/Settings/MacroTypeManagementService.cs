using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Domain.Services.Settings;

public class MacroTypeManagementService : IMacroTypeManagementService
{
    private readonly DataBaseContext _context;
    private readonly ILogger<MacroTypeManagementService> _logger;

    public MacroTypeManagementService(DataBaseContext context, ILogger<MacroTypeManagementService> logger)
    {
        _context = context;
        this._logger = logger;
    }

    public async Task<MacroType> AddMacroTypeAsync(MacroType macroType)
    {
        _logger.LogInformation("Adding new macro type with ID: {MacroTypeId}", macroType.Id);
        await _context.MacroType.AddAsync(macroType);
        return macroType;
    }

    public async Task<MacroType> DeleteMacroTypeAsync(Guid id)
    {
        _logger.LogInformation("Deleting macro type with ID: {MacroTypeId}", id);
        var macroType = await _context.MacroType.FindAsync(id);
        
        if (macroType == null)
        {
            _logger.LogWarning("MacroType with ID: {MacroTypeId} not found for deletion", id);
            throw new InvalidOperationException($"MacroType with ID {id} not found");
        }

        _context.MacroType.Remove(macroType);
        return macroType;
    }

    public async Task<MacroType> GetMacroTypeAsync(Guid id)
    {
        _logger.LogInformation("Retrieving macro type with ID: {MacroTypeId}", id);
        var macroType = await _context.MacroType.FindAsync(id);
        
        if (macroType == null)
        {
            _logger.LogWarning("MacroType with ID: {MacroTypeId} not found", id);
            throw new InvalidOperationException($"MacroType with ID {id} not found");
        }

        return macroType;
    }

    public async Task<List<MacroType>> GetMacroTypeListAsync()
    {
        _logger.LogInformation("Retrieving all macro types");
        return await _context.MacroType.ToListAsync();
    }

    public async Task<bool> MacroTypeExistsAsync(Guid id)
    {
        return await _context.MacroType.AnyAsync(e => e.Id == id);
    }

    public async Task<MacroType> UpdateMacroTypeAsync(MacroType macroType)
    {
        _logger.LogInformation("Updating macro type with ID: {MacroTypeId}", macroType.Id);
        _context.MacroType.Update(macroType);
        return macroType;
    }
}