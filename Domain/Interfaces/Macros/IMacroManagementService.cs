using Klacks.Api.Domain.Models.Settings;

namespace Klacks.Api.Domain.Interfaces.Macros;

public interface IMacroManagementService
{
    Task<Macro> AddMacroAsync(Macro macro);

    Task<Macro> DeleteMacroAsync(Guid id);

    Task<Macro> GetMacroAsync(Guid id);

    Task<List<Macro>> GetMacroListAsync();

    Task<bool> MacroExistsAsync(Guid id);

    Task<Macro> UpdateMacroAsync(Macro macro);
}