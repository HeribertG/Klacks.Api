// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.KnowledgeIndex.Application.Interfaces;

public interface IKnowledgeIndexSynchronizer
{
    Task SyncAsync(CancellationToken cancellationToken);
}
