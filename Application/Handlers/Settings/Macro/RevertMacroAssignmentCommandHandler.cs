// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Undoes a recorded macro switch as a whole (owner decisions F2 and F6). The undo is planned first through the same
/// preview the confirmation used, dry run included (a refusal becomes an InvalidRequestException: unknown, itself an undo,
/// already undone, any row in conflict with the current state, listed, or a macro to restore that cannot run); then, in
/// one transaction, the rows of the switch are read and matched to the planned changes by holder. Because those rows may
/// already be tracked from planning, the handler also checks untracked that each holder's latest recorded switch is still
/// the one being undone and that the holder still carries the reference the plan started from; a row that vanished,
/// appeared or was undone, a later switch or an outside change aborts without writing. Each holder then gets its previous
/// reference (possibly none: Guid.Empty counts as no macro, as in production, and is restored as null) back on its row
/// alone, one undo row per holder is recorded under a new shared switch id and points at the row it undid, every undone
/// row is marked, and every written holder is read back (a mismatch rolls everything back). The undo rows are built once,
/// before the transaction, so a retry of the transaction re-adds the same rows. The outcome carries the dry run of the
/// preview and the id of the undone switch. Nothing is recalculated; an undo itself is final. A failure while saving
/// (any exception of the transaction other than a refusal or a cancellation) is logged as an error with the undo and
/// switch ids, the holder id and the holder count, never with names, and rethrown for the caller to map.
/// </summary>
/// <param name="request">Which switch to undo and the requesting user</param>

using Klacks.Api.Application.Commands.Settings.Macros;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Macros;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Macros;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Settings.Macro;

public class RevertMacroAssignmentCommandHandler : IRequestHandler<RevertMacroAssignmentCommand, MacroAssignmentOutcome>
{
    private const string SaveFailedLogMessage =
        "Undo {UndoSwitchId} of macro switch {UndoneSwitchId} on {Target} {HolderId} with {HolderCount} holder(s) failed "
        + "to save";
    private const string SwitchChangedMessage =
        "The macro switch to undo is no longer recorded as it was planned; nothing was changed.";

    private readonly IMacroAssignmentPlanner _planner;
    private readonly IMacroReferenceRepository _references;
    private readonly IMacroAssignmentHistoryRepository _history;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RevertMacroAssignmentCommandHandler> _logger;

    public RevertMacroAssignmentCommandHandler(
        IMacroAssignmentPlanner planner,
        IMacroReferenceRepository references,
        IMacroAssignmentHistoryRepository history,
        IUnitOfWork unitOfWork,
        ILogger<RevertMacroAssignmentCommandHandler> logger)
    {
        _planner = planner;
        _references = references;
        _history = history;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<MacroAssignmentOutcome> Handle(
        RevertMacroAssignmentCommand request, CancellationToken cancellationToken)
    {
        var preview = await _planner.PreviewRevertAsync(
            new MacroRevertRequest(request.SwitchId, request.ShiftId, request.AbsenceTypeId), cancellationToken);
        if (preview.Refusal != null)
        {
            throw new InvalidRequestException(preview.Refusal);
        }

        var plan = preview.Plan;
        var undoneSwitchId = plan.SwitchId!.Value;
        var undoSwitchId = Guid.NewGuid();
        var undoRows = plan.Changes.Select(change => NewUndoRow(undoSwitchId, change, request.UserId)).ToList();

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var undoneRows = await _history.GetSwitchAsync(undoneSwitchId, cancellationToken);
                if (undoneRows.Count != plan.Changes.Count)
                {
                    throw new InvalidRequestException(SwitchChangedMessage);
                }

                foreach (var (change, undo) in plan.Changes.Zip(undoRows))
                {
                    await UndoAsync(change, undo, undoneRows, undoneSwitchId, cancellationToken);
                }

                await _unitOfWork.CompleteAsync();
                await MacroAssignmentVerification.VerifyAsync(_references, plan.Changes, cancellationToken);
                return true;
            });
        }
        catch (Exception ex) when (ex is not InvalidRequestException and not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                SaveFailedLogMessage,
                undoSwitchId,
                undoneSwitchId,
                plan.Holder!.Target,
                plan.Holder.Id,
                plan.Changes.Count);
            throw;
        }

        return new MacroAssignmentOutcome(
            undoSwitchId, plan.Holder!, plan.Changes, plan.Warnings, preview.DryRun!, undoneSwitchId);
    }

    private async Task UndoAsync(
        MacroReferenceChange change,
        MacroAssignmentHistory undo,
        IReadOnlyList<MacroAssignmentHistory> undoneRows,
        Guid undoneSwitchId,
        CancellationToken cancellationToken)
    {
        var undone = undoneRows.FirstOrDefault(row =>
            row.Target == change.Holder.Target && row.TargetId == change.Holder.Id);
        if (undone == null || (undone.RevertedByHistoryId.HasValue && undone.RevertedByHistoryId != undo.Id))
        {
            throw new InvalidRequestException(SwitchChangedMessage);
        }

        var latest = await _history.GetLatestAsync(change.Holder.Target, change.Holder.Id, cancellationToken);
        if (latest?.SwitchId != undoneSwitchId)
        {
            throw new InvalidRequestException(SwitchChangedMessage);
        }

        await MacroAssignmentVerification.EnsureUnchangedSincePlanAsync(_references, change, cancellationToken);
        var written = await _references.SetMacroIdAsync(
            change.Holder.Target, change.Holder.Id, change.ToMacroId, cancellationToken);
        MacroAssignmentVerification.EnsureHolderStillExists(written, change.Holder);
        undo.RevertOfHistoryId = undone.Id;
        _history.Add(undo);
        undone.RevertedByHistoryId = undo.Id;
    }

    private static MacroAssignmentHistory NewUndoRow(Guid undoSwitchId, MacroReferenceChange change, Guid userId) =>
        new()
        {
            Id = Guid.NewGuid(),
            SwitchId = undoSwitchId,
            Target = change.Holder.Target,
            TargetId = change.Holder.Id,
            PreviousMacroId = change.FromMacroId,
            NewMacroId = change.ToMacroId,
            ChangedByUserId = userId
        };
}
