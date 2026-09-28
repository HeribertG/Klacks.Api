// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.KnowledgeIndex.Application.Interfaces;

public interface IKnowledgeIndexSynchronizer
{
    Task SyncAsync(CancellationToken cancellationToken);
}
