using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.Macros;

public interface IBreakMacroService
{
    Task ProcessBreakMacroAsync(Break breakEntry);
}
