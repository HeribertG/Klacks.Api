using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Infrastructure.Repositories;

public class ContractRepository : BaseRepository<Contract>, IContractRepository
{
    public ContractRepository(DataBaseContext context, ILogger<Contract> logger) 
        : base(context, logger)
    {
    }
}