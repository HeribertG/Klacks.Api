// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ISubstratePriorDeriver
{
    Task DeriveAsync(CancellationToken cancellationToken = default);
}
