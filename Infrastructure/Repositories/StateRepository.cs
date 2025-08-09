using Klacks.Api.Datas;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Models.Settings;

namespace Klacks.Api.Infrastructure.Repositories;

public class StateRepository : BaseRepository<State>, IStateRepository
{
    private readonly DataBaseContext context;

    public StateRepository(DataBaseContext context, ILogger<State> logger)
        : base(context, logger)
    {
        this.context = context;
    }
}
