// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reads shifts and absence types as macro holders, the cut group of a shift and macros as snapshots (all untracked), and
/// changes the macro reference of exactly one holder per call. Stage-only: SetMacroIdAsync changes a tracked row, the
/// caller commits through IUnitOfWork.
/// </summary>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Macros;

namespace Klacks.Api.Domain.Interfaces.Macros;

public interface IMacroReferenceRepository
{
    Task<MacroReferenceHolder?> FindHolderAsync(
        MacroAssignmentTarget target, Guid holderId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MacroReferenceHolder>> FindCutGroupAsync(
        Guid cutGroupKey, CancellationToken cancellationToken = default);

    Task<MacroSnapshot?> FindMacroAsync(Guid macroId, CancellationToken cancellationToken = default);

    Task<bool> SetMacroIdAsync(
        MacroAssignmentTarget target, Guid holderId, Guid? macroId, CancellationToken cancellationToken = default);
}
