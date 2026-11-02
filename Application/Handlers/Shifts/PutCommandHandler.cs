// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Shifts;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Domain.Services.Shifts;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Shifts;

/// <summary>
/// Saves a shift from the edit form. A shift a group-restricted caller may not see is answered exactly like a missing
/// one (null, i.e. 404), before any other check; adding, removing or re-dating an assignment to a hidden group is
/// refused like a missing group. A changed customer must exist, must not be soft-deleted and must be visible, otherwise
/// the save is refused with the same 404 for all three; an unchanged customer is not checked, so a shift whose stored
/// customer the caller cannot see stays saveable. A stored draft order (OriginalOrder) may only be saved as a draft or
/// sealed (SealedOrder); any other status is refused with 400, because the status is server-owned hierarchy. The lock
/// check runs first, then the draft status check, then the customer check, so a locked shift answers 409 and a forbidden
/// draft status 400 whatever customer was sent. A stored shift of a variant (key) is checked by IVariantShiftWriteGuard
/// from its stored token, so a body without a token gets the same answer. Such a shift is a pair of rows, the sealed order
/// and its plannable copy: a PUT addressed at either row is saved on that row and the repository mirrors the mirrored
/// fields (VariantShiftMirror), the group items, the expenses and the required qualifications onto the other row in the
/// same save, so the answer is the addressed row. While the order has cuts, a change of its date, time, weekdays,
/// staffing, customer, macro or type is refused with a conflict (the guard); name, abbreviation, description and groups
/// stay changeable. Setting the still empty end date of a real locked shift ends its whole family (IShiftFamilyEndService):
/// the addressed row is saved inside that transaction, and a refusal answers 409 before anything is written.
/// </summary>
/// <param name="shiftVisibilityGuard">Decides whether the caller may touch the shift and its changed groups</param>
/// <param name="clientReferenceGuard">Decides whether a changed customer exists and is visible to the caller</param>
/// <param name="variantShiftWriteGuard">Refuses an edit of a shift in a consumed or hidden variant and content a variant does not accept</param>
/// <param name="shiftFamilyEndService">Ends a whole family when the save sets the first end date of a real locked shift</param>
public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<ShiftResource>, ShiftResource?>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IShiftRequiredQualificationRepository _requirementRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IShiftVisibilityGuard _shiftVisibilityGuard;
    private readonly IClientReferenceGuard _clientReferenceGuard;
    private readonly IVariantShiftWriteGuard _variantShiftWriteGuard;
    private readonly IShiftFamilyEndService _shiftFamilyEndService;

    public PutCommandHandler(
        IShiftRepository shiftRepository,
        IShiftRequiredQualificationRepository requirementRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        IShiftVisibilityGuard shiftVisibilityGuard,
        IClientReferenceGuard clientReferenceGuard,
        IVariantShiftWriteGuard variantShiftWriteGuard,
        IShiftFamilyEndService shiftFamilyEndService,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _shiftRepository = shiftRepository;
        _requirementRepository = requirementRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
        _shiftVisibilityGuard = shiftVisibilityGuard;
        _clientReferenceGuard = clientReferenceGuard;
        _variantShiftWriteGuard = variantShiftWriteGuard;
        _shiftFamilyEndService = shiftFamilyEndService;
    }

    public async Task<ShiftResource?> Handle(PutCommand<ShiftResource> request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating shift: Id={ShiftId}, Name={Name}, MacroId={MacroId}, ClientId={ClientId}, GroupCount={GroupCount}",
            request.Resource.Id, request.Resource.Name, request.Resource.MacroId, request.Resource.ClientId, request.Resource.Groups?.Count ?? 0);

        if (!await _shiftVisibilityGuard.IsVisibleAsync(request.Resource.Id, cancellationToken))
        {
            _logger.LogWarning("Shift not found for update: Id={ShiftId}", request.Resource.Id);
            return null;
        }

        var shift = _scheduleMapper.ToShiftEntity(request.Resource);
        await _shiftVisibilityGuard.EnsureGroupChangesVisibleAsync(shift.Id, shift.GroupItems, cancellationToken);
        var stored = await _shiftRepository.GetNoTracking(shift.Id);
        if (stored != null)
        {
            await _variantShiftWriteGuard.EnsureEditableAsync(stored, shift, cancellationToken);
        }

        EnsureNotLocked(stored, shift);
        EnsureDraftStatusTransitionAllowed(stored, shift);
        await EnsureChangedClientExistsVisibleAsync(stored, shift, cancellationToken);
        await KeepInheritedRequirementsAsync(shift, cancellationToken);

        var resultShift = await SaveAsync(stored, shift, cancellationToken);

        if (resultShift == null)
        {
            _logger.LogWarning("Shift not found for update: Id={ShiftId}", request.Resource.Id);
            return null;
        }

        var freshShift = await _shiftRepository.Get(resultShift.Id);

        _logger.LogInformation("Shift updated successfully: Id={ShiftId}, Name={Name}, Status={Status}",
            resultShift.Id, resultShift.Name, resultShift.Status);

        return _scheduleMapper.ToShiftResource(freshShift ?? resultShift);
    }

    /// <summary>
    /// Saves and commits the shift: a save that sets the first end date of a real locked shift together with its family,
    /// any other save on the addressed row alone.
    /// </summary>
    /// <param name="stored">Shift as saved, or null when it does not exist</param>
    /// <param name="shift">Shift as mapped from the request</param>
    private async Task<Shift?> SaveAsync(Shift? stored, Shift shift, CancellationToken cancellationToken)
    {
        if (stored == null || !LockedShiftEditPolicy.SetsFamilyEnd(stored, shift))
        {
            var saved = await _shiftRepository.PutWithSealedOrderHandling(shift);
            if (saved != null)
            {
                await _unitOfWork.CompleteAsync();
            }

            return saved;
        }

        Shift? savedEntry = null;
        await _shiftFamilyEndService.EndFamilyAsync(
            stored,
            shift.UntilDate!.Value,
            async () => savedEntry = await _shiftRepository.PutWithSealedOrderHandling(shift),
            cancellationToken);
        return savedEntry;
    }

    /// <summary>
    /// Refuses a save that changes content of a shift that is no longer a draft order, with the same fields the edit form
    /// disables. The sealing save (stored draft order to sealed order) passes because the stored row is still a draft.
    /// </summary>
    /// <param name="stored">Shift as saved, or null when it does not exist</param>
    /// <param name="incoming">Shift as mapped from the request</param>
    private void EnsureNotLocked(Shift? stored, Shift incoming)
    {
        if (stored == null)
        {
            return;
        }

        var lockedChanges = LockedShiftEditPolicy.FindLockedChanges(stored, incoming);
        if (lockedChanges.Count > 0)
        {
            _logger.LogWarning("Shift update refused: Id={ShiftId}, Status={Status}, LockedFields={LockedFields}",
                stored.Id, stored.Status, string.Join(',', lockedChanges));
            throw ShiftLockedException.ForEdit(stored, lockedChanges);
        }
    }

    /// <summary>
    /// A draft order becomes something else only by sealing (which derives the plannable copy); turning it into a
    /// plannable shift or a cut piece by PUT would bypass the server-owned hierarchy.
    /// </summary>
    /// <param name="stored">Shift as saved, or null when it does not exist</param>
    /// <param name="incoming">Shift as mapped from the request</param>
    private void EnsureDraftStatusTransitionAllowed(Shift? stored, Shift incoming)
    {
        if (stored?.Status != ShiftStatus.OriginalOrder
            || incoming.Status is ShiftStatus.OriginalOrder or ShiftStatus.SealedOrder)
        {
            return;
        }

        _logger.LogWarning("Shift update refused: draft order {ShiftId} sent with status {Status}", stored.Id, incoming.Status);
        throw new BadRequestException(
            string.Format(ShiftHierarchyMessages.DraftStatusChangeNotAllowed, stored.Id, incoming.Status));
    }

    /// <summary>
    /// Checks the customer only when the save sets a different one than stored; a missing shift is left to the
    /// repository's not-found answer, so it never reports a customer.
    /// </summary>
    /// <param name="stored">Shift as saved, or null when it does not exist</param>
    /// <param name="incoming">Shift as mapped from the request</param>
    private async Task EnsureChangedClientExistsVisibleAsync(Shift? stored, Shift incoming, CancellationToken cancellationToken)
    {
        if (stored == null || !incoming.ClientId.HasValue || incoming.ClientId == stored.ClientId)
        {
            return;
        }

        await _clientReferenceGuard.EnsureExistsVisibleAsync(incoming.ClientId.Value, cancellationToken);
    }

    /// <summary>
    /// The form shows and saves only a shift's own requirement rows. When it saves the first own rows of a shift that
    /// so far inherited its requirements (cut ancestor, plannable copy, sealed order), the inherited rows are added as
    /// own rows, so the planner's addition does not silently replace them (nearest link wins). A save without rows
    /// keeps inheriting; a row the form sends for an inherited qualification wins over the inherited values.
    /// </summary>
    /// <param name="shift">Shift as mapped from the form; its RequiredQualifications list is extended in place</param>
    private async Task KeepInheritedRequirementsAsync(Shift shift, CancellationToken cancellationToken)
    {
        if (shift.RequiredQualifications.Count == 0)
        {
            return;
        }

        var effective = await _requirementRepository.GetEffectiveByShiftIdsAsync([shift.Id], cancellationToken);
        var sent = shift.RequiredQualifications.Select(row => row.QualificationId).ToHashSet();
        shift.RequiredQualifications.AddRange(
            ShiftRequirementMaterializer.InheritedAsOwn(effective, shift.Id).Where(row => !sent.Contains(row.QualificationId)));
    }
}
