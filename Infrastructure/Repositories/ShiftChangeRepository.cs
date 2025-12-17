using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;

namespace Klacks.Api.Infrastructure.Repositories;

public class ShiftChangeRepository : BaseRepository<ShiftChange>, IShiftChangeRepository
{
    public ShiftChangeRepository(DataBaseContext context, ILogger<ShiftChange> logger)
        : base(context, logger)
    {
    }
}
