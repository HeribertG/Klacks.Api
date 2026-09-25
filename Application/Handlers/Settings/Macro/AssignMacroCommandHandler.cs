// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Switches the macro reference of an absence type, or of a shift together with every other live shift cut from the same
/// order (owner decision F2). The switch is planned first through the same preview the confirmation used, dry run
/// included, so a refusal or a target macro that cannot run becomes an InvalidRequestException and the dry-run warnings
/// and the dry run itself reach the outcome (the skill reports it without a second run). Then, in one transaction, each changed holder is re-read untracked (vanished or changed since the
/// plan aborts everything), its reference is written on its row alone, one history row per written holder records the
/// macro before and after, the requesting user and the shared switch id, and every written holder is read back (a
/// mismatch rolls everything back). The switch id and the history rows are built once, before the transaction, so a
/// retry of the transaction by the execution strategy re-adds the same rows instead of duplicating them. Nothing is
/// recalculated: works and breaks keep their stored values until they are edited or recalculated (sealed ones are
/// skipped by recalculations), and no macro row is written. A failure while saving (any exception of the transaction
/// other than a refusal or a cancellation) is logged as an error with the switch, holder and macro ids and the holder
/// count, never with names, and rethrown for the caller to map.
/// </summary>
/// <param name="request">Holder kind and id, the new macro and the requesting user</param>

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

public class AssignMacroCommandHandler : IRequestHandler<AssignMacroCommand, MacroAssignmentOutcome>
{
    private const string SaveFailedLogMessage =
        "Macro switch {SwitchId} of {Target} {HolderId} to macro {MacroId} with {HolderCount} holder(s) failed to save";

    private readonly IMacroAssignmentPlanner _planner;
    private readonly IMacroReferenceRepository _references;
    private readonly IMacroAssignmentHistoryRepository _history;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AssignMacroCommandHandler> _logger;

    public AssignMacroCommandHandler(
        IMacroAssignmentPlanner planner,
        IMacroReferenceRepository references,
        IMacroAssignmentHistoryRepository history,
        IUnitOfWork unitOfWork,
        ILogger<AssignMacroCommandHandler> logger)
    {
        _planner = planner;
        _references = references;
        _history = history;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<MacroAssignmentOutcome> Handle(AssignMacroCommand request, CancellationToken cancellationToken)
    {
        var preview = await _planner.PreviewAssignAsync(
            request.Target, request.HolderId, request.MacroId, cancellationToken);
        if (preview.Refusal != null)
        {
            throw new InvalidRequestException(preview.Refusal);
        }

        var plan = preview.Plan;
        var switchId = Guid.NewGuid();
        var entries = plan.Changes.Select(change => NewEntry(switchId, change, request.UserId)).ToList();

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                foreach (var change in plan.Changes)
                {
                    await MacroAssignmentVerification.EnsureUnchangedSincePlanAsync(_references, change, cancellationToken);
                    var written = await _references.SetMacroIdAsync(
                        change.Holder.Target, change.Holder.Id, change.ToMacroId, cancellationToken);
                    MacroAssignmentVerification.EnsureHolderStillExists(written, change.Holder);
                }

                entries.ForEach(_history.Add);
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
                switchId,
                request.Target,
                request.HolderId,
                request.MacroId,
                plan.Changes.Count);
            throw;
        }

        return new MacroAssignmentOutcome(switchId, plan.Holder!, plan.Changes, plan.Warnings, preview.DryRun!, null);
    }

    private static MacroAssignmentHistory NewEntry(Guid switchId, MacroReferenceChange change, Guid userId) =>
        new()
        {
            Id = Guid.NewGuid(),
            SwitchId = switchId,
            Target = change.Holder.Target,
            TargetId = change.Holder.Id,
            PreviousMacroId = change.FromMacroId,
            NewMacroId = change.ToMacroId,
            ChangedByUserId = userId
        };
}
