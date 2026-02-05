using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

public class ExpensesRepository : BaseRepository<Expenses>, IExpensesRepository
{
    public ExpensesRepository(DataBaseContext context, ILogger<Expenses> logger)
        : base(context, logger)
    {
    }
}
