using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces;

public interface IWorkMacroService
{
    Task ProcessWorkMacroAsync(Work work);
}
