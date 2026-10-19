// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Imports;

namespace Klacks.Api.Domain.Interfaces.Imports;

public interface IErpImportExceptionRepository : IBaseRepository<ErpImportException>
{
    Task<List<ErpImportException>> GetOpenAsync(CancellationToken cancellationToken = default);

    Task<List<ErpImportException>> GetByFileKeysAsync(IReadOnlyCollection<string> fileKeys, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the open (unresolved) exceptions that name an order and are linked to one of the given ledger
    /// rows, untracked, oldest first. These are the rejected orders of the files; the order-less exception of
    /// a Failed file is left out (its reason is the row's failure reason).
    /// </summary>
    Task<List<ErpImportException>> GetOpenRejectedOrdersAsync(IReadOnlyCollection<Guid> importFileIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Self-committing (single conditional UPDATE through ExecuteUpdateAsync, bypasses the change tracker):
    /// marks every open exception of the ledger row as resolved at resolvedAt. Returns the number of rows.
    /// </summary>
    Task<int> ResolveOpenForImportFileAsync(Guid importFileId, DateTime resolvedAt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Self-committing (single conditional UPDATE through ExecuteUpdateAsync): links every exception recorded
    /// for fileKey that is not linked yet to the ledger row. Used when a pre-ledger file is migrated into the
    /// ledger. Returns the number of rows.
    /// </summary>
    Task<int> LinkToImportFileAsync(string fileKey, Guid importFileId, CancellationToken cancellationToken = default);
}
