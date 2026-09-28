// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Common;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Infrastructure.Repositories.Staffs;

public class MembershipRepository : BaseRepository<Membership>, IMembershipRepository
{
    public MembershipRepository(DataBaseContext context, ILogger<Membership> logger)
      : base(context, logger)
    {
    }
}
