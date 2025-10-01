using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Domain.Services.Settings;

public class MacroManagementService : IMacroManagementService
{
    private readonly DataBaseContext _context;
    private readonly ILogger<MacroManagementService> _logger;

    public MacroManagementService(DataBaseContext context, ILogger<MacroManagementService> logger)
    {
        _context = context;
        this._logger = logger;
    }

    public async Task<Macro> AddMacroAsync(Macro macro)
    {
        _logger.LogInformation("Adding new macro with ID: {MacroId}", macro.Id);
        await _context.Macro.AddAsync(macro);
        return macro;
    }

    public async Task<Macro> DeleteMacroAsync(Guid id)
    {
        _logger.LogInformation("Deleting macro with ID: {MacroId}", id);
        var macro = await _context.Macro.FindAsync(id);
        
        if (macro == null)
        {
            _logger.LogWarning("Macro with ID: {MacroId} not found for deletion", id);
            throw new InvalidOperationException($"Macro with ID {id} not found");
        }

        _context.Macro.Remove(macro);
        return macro;
    }

    public async Task<Macro> GetMacroAsync(Guid id)
    {
        _logger.LogInformation("Retrieving macro with ID: {MacroId}", id);
        var macro = await _context.Macro.FindAsync(id);
        
        if (macro == null)
        {
            _logger.LogWarning("Macro with ID: {MacroId} not found", id);
            throw new InvalidOperationException($"Macro with ID {id} not found");
        }

        return macro;
    }

    public async Task<List<Macro>> GetMacroListAsync()
    {
        _logger.LogInformation("Retrieving all macros");
        return await _context.Macro.ToListAsync();
    }

    public async Task<bool> MacroExistsAsync(Guid id)
    {
        return await _context.Macro.AnyAsync(e => e.Id == id);
    }

    public async Task<Macro> UpdateMacroAsync(Macro macro)
    {
        _logger.LogInformation("Updating macro with ID: {MacroId}", macro.Id);
        _context.Macro.Update(macro);
        return macro;
    }
}