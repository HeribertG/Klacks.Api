// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Imports;

namespace Klacks.Api.Domain.Interfaces.Imports;

public interface IErpImportExceptionRepository : IBaseRepository<ErpImportException>
{
    Task<List<ErpImportException>> GetOpenAsync(CancellationToken cancellationToken = default);

    Task<List<ErpImportException>> GetByFileKeysAsync(IReadOnlyCollection<string> fileKeys, CancellationToken cancellationToken = default);
}
