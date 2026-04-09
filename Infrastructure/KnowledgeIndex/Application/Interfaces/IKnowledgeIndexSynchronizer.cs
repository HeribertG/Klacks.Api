// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Infrastructure.KnowledgeIndex.Application.Interfaces;

public interface IKnowledgeIndexSynchronizer
{
    Task SyncAsync(CancellationToken cancellationToken);
}
