using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;

namespace Klacks.Api.Infrastructure.Repositories;

public class AbsenceDetailRepository : BaseRepository<AbsenceDetail>, IAbsenceDetailRepository
{
    public AbsenceDetailRepository(DataBaseContext context, ILogger<AbsenceDetail> logger)
        : base(context, logger)
    {
    }
}
