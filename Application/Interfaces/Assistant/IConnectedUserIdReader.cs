// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Interfaces.Assistant;

/// <summary>
/// The ids of the users holding an open connection to the assistant hub of this server process. A user without an open
/// browser tab is not counted, so this is not the number of signed-in accounts.
/// </summary>
public interface IConnectedUserIdReader
{
    Task<IReadOnlyList<string>> GetConnectedUserIdsAsync(CancellationToken cancellationToken = default);
}
