// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
