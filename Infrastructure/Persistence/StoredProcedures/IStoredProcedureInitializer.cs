// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Infrastructure.Persistence.StoredProcedures;

public interface IStoredProcedureInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
