using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Scheduling;
using Klacks.Api.Infrastructure.Persistence;

namespace Klacks.Api.Infrastructure.Repositories.Scheduling;

public class SchedulingRuleRepository : BaseRepository<SchedulingRule>, ISchedulingRuleRepository
{
    public SchedulingRuleRepository(DataBaseContext context, ILogger<SchedulingRule> logger)
        : base(context, logger)
    {
    }
}
