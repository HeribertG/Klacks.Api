using Klacks.Api.Domain.Models.Macros;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces;

public interface IMacroDataProvider
{
    Task<MacroData> GetMacroDataAsync(Work work);
}
