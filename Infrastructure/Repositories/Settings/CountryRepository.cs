// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Settings;

namespace Klacks.Api.Infrastructure.Repositories.Settings;

public class CountryRepository : BaseRepository<Countries>, ICountryRepository
{
    private readonly DataBaseContext context;

    public CountryRepository(DataBaseContext context, ILogger<Countries> logger)
        : base(context, logger)
    {
        this.context = context;
    }
}
