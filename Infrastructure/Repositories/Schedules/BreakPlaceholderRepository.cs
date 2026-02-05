using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;

namespace Klacks.Api.Infrastructure.Repositories
{
    public class BreakPlaceholderRepository : BaseRepository<BreakPlaceholder>, IBreakPlaceholderRepository
    {
        public BreakPlaceholderRepository(DataBaseContext context, ILogger<BreakPlaceholder> logger)
          : base(context, logger)
        {
        }
    }
}
