// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Interfaces.Schedules;

public interface IContainerBaseAddressResolver
{
    Task<string> ResolveAsync(string? storedBase, CancellationToken cancellationToken);
}
