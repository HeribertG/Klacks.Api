using Klacks.Api.Domain.Common;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Settings;

namespace Klacks.Api.Infrastructure.Repositories.Settings;

public class BranchRepository : BaseRepository<Branch>, IBranchRepository
{
    private readonly DataBaseContext context;

    public BranchRepository(DataBaseContext context, ILogger<Branch> logger)
        : base(context, logger)
    {
        this.context = context;
    }
}
