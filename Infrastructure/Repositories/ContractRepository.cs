using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Infrastructure.Repositories;

public class ContractRepository : BaseRepository<Contract>, IContractRepository
{
    private readonly DataBaseContext context;

    public ContractRepository(DataBaseContext context, ILogger<Contract> logger) 
        : base(context, logger)
    {
        this.context = context;
    }

    public override async Task<Contract?> Get(Guid id)
    {
        return await context.Set<Contract>()
            .Include(c => c.CalendarSelection)
            .FirstOrDefaultAsync(c => c.Id == id);
    }
}