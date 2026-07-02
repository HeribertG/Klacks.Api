// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Interfaces.Imports;

public interface IErpOrderImportRunner
{
    Task RunAsync(CancellationToken cancellationToken = default);
}
