using Klacks.Api.Domain.Models.Settings;

namespace Klacks.Api.Domain.Interfaces;

public interface IMacroTypeManagementService
{
    Task<MacroType> AddMacroTypeAsync(MacroType macroType);
    Task<MacroType> DeleteMacroTypeAsync(Guid id);
    Task<MacroType> GetMacroTypeAsync(Guid id);
    Task<List<MacroType>> GetMacroTypeListAsync();
    Task<bool> MacroTypeExistsAsync(Guid id);
    Task<MacroType> UpdateMacroTypeAsync(MacroType macroType);
}