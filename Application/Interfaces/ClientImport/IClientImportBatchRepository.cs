// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Application.Interfaces.ClientImport;

public interface IClientImportBatchRepository
{
    Task<bool> ExistsByTokenAsync(Guid token, CancellationToken cancellationToken);

    Task AddAsync(ClientImportBatch batch, CancellationToken cancellationToken);
}
