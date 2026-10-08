// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default <see cref="IMembershipWindowReader"/>, backed by <see cref="MembershipWindowQuery"/>.
/// </summary>
/// <param name="context">EF Core context holding the memberships</param>

using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Infrastructure.Persistence;

namespace Klacks.Api.Infrastructure.Repositories.Associations;

public sealed class MembershipWindowReader : IMembershipWindowReader
{
    private readonly DataBaseContext _context;

    public MembershipWindowReader(DataBaseContext context)
    {
        _context = context;
    }

    public Task<IReadOnlyDictionary<Guid, MembershipWindow>> GetWindowsAsync(
        IReadOnlyCollection<Guid> clientIds,
        CancellationToken cancellationToken)
        => MembershipWindowQuery.LoadAsync(_context, clientIds, cancellationToken);
}