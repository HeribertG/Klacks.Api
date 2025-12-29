using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;

namespace Klacks.Api.Infrastructure.Repositories;

public class BreakRepository : BaseRepository<Break>, IBreakRepository
{
    public BreakRepository(DataBaseContext context, ILogger<Break> logger)
        : base(context, logger)
    {
    }
}
