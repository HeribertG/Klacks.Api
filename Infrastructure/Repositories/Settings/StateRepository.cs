// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Common;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Settings;

namespace Klacks.Api.Infrastructure.Repositories.Settings;

public class StateRepository : BaseRepository<State>, IStateRepository
{
    public StateRepository(DataBaseContext context, ILogger<State> logger)
        : base(context, logger)
    {
    }
}
