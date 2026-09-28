// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Imports;
using Klacks.Api.Infrastructure.Persistence;

namespace Klacks.Api.Infrastructure.Repositories.Imports;

public class ErpDropPointRepository : BaseRepository<ErpDropPoint>, IErpDropPointRepository
{
    public ErpDropPointRepository(DataBaseContext context, ILogger<ErpDropPoint> logger)
        : base(context, logger)
    {
    }
}
