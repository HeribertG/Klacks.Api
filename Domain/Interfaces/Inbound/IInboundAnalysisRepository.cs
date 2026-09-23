// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Inbound;

namespace Klacks.Api.Domain.Interfaces.Inbound;

public interface IInboundAnalysisRepository
{
    Task AddAsync(InboundAnalysis analysis, CancellationToken cancellationToken = default);

    Task<InboundAnalysis?> GetBySourceAsync(
        InboundSourceKind sourceKind, Guid sourceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Existence check that ignores soft-delete: the unique index on (SourceKind, SourceId) has no
    /// is_deleted filter, so a soft-deleted row still blocks a re-insert. GetBySourceAsync applies the
    /// global query filter and would miss such a row, letting the caller retry an insert that always
    /// fails. Callers use this before adding a new analysis for a source.
    /// </summary>
    Task<bool> ExistsBySourceAsync(
        InboundSourceKind sourceKind, Guid sourceId, CancellationToken cancellationToken = default);
}
