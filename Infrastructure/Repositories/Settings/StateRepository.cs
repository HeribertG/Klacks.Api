// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Settings;

namespace Klacks.Api.Infrastructure.Repositories.Settings;

public class StateRepository : BaseRepository<State>, IStateRepository
{
    private readonly DataBaseContext context;

    public StateRepository(DataBaseContext context, ILogger<State> logger)
        : base(context, logger)
    {
        this.context = context;
    }
}
