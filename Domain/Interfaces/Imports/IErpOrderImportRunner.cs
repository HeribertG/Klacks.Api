// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Interfaces.Imports;

public interface IErpOrderImportRunner
{
    Task RunAsync(CancellationToken cancellationToken = default);
}
