// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Persistence of the macro assignment history. Stage-only: Add registers a row, the caller commits through IUnitOfWork.
/// GetSwitchAsync returns all rows of one switch tracked (an undo marks the rows it undid); GetLatestAsync reads untracked.
/// </summary>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Settings;

namespace Klacks.Api.Domain.Interfaces.Settings;

public interface IMacroAssignmentHistoryRepository
{
    void Add(MacroAssignmentHistory entry);

    Task<IReadOnlyList<MacroAssignmentHistory>> GetSwitchAsync(Guid switchId, CancellationToken cancellationToken = default);

    Task<MacroAssignmentHistory?> GetLatestAsync(
        MacroAssignmentTarget target, Guid targetId, CancellationToken cancellationToken = default);
}
