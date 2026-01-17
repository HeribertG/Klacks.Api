using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Interfaces;

public interface IBreakContextRepository : IBaseRepository<BreakContext>
{
    Task SoftDeleteByAbsenceIdAsync(Guid absenceId);
}
