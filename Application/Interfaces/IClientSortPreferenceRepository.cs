// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Repository for reading and replacing per-user, per-group client sort preferences.
/// </summary>

using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Application.Interfaces;

public interface IClientSortPreferenceRepository
{
    Task<List<ClientSortPreference>> GetByUserAndGroupAsync(
        string userId, Guid groupId, CancellationToken cancellationToken = default);

    Task ReplaceAllAsync(
        string userId, Guid groupId, IEnumerable<ClientSortPreference> newEntries);
}
