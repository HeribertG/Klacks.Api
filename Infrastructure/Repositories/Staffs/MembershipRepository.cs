// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Infrastructure.Repositories.Staffs;

public class MembershipRepository : BaseRepository<Membership>, IMembershipRepository
{
    private readonly DataBaseContext context;

    public MembershipRepository(DataBaseContext context, ILogger<Membership> logger)
      : base(context, logger)
    {
        this.context = context;
    }
}
