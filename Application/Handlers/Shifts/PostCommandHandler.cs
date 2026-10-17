// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Creates one shift from the UI. A new shift is always an order (draft or sealed); the hierarchy is server-owned, so
/// order reference and cut-tree fields of the request are discarded and any other status is refused with 400.
/// Enforces ClientlessOrderRules before anything is written: a draft
/// without a customer is refused outright, and an order that deliberately has none must already meet
/// the sealing requirements, because it is sealed on creation and sealing cannot be undone.
/// </summary>
/// <param name="shiftRepository">Persists the shift and derives the plannable shift when it is sealed.</param>
/// <param name="scheduleMapper">Maps the incoming resource to the entity.</param>
/// <param name="unitOfWork">Commits the write.</param>
/// <param name="defaultShiftMacroResolver">Fills in the standard calculation macro when none was chosen.</param>
/// <param name="orderSealingService">Source of the sealing requirements a clientless order must satisfy.</param>
/// <param name="shiftVisibilityGuard">Refuses a group-restricted caller every group they cannot see, like a missing group.</param>
/// <param name="logger">Structured log per creation.</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Services.Shifts;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Services.Shifts;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Shifts;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<ShiftResource>, ShiftResource?>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDefaultShiftMacroResolver _defaultShiftMacroResolver;
    private readonly IOrderSealingService _orderSealingService;
    private readonly IShiftVisibilityGuard _shiftVisibilityGuard;

    public PostCommandHandler(
        IShiftRepository shiftRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        IDefaultShiftMacroResolver defaultShiftMacroResolver,
        IOrderSealingService orderSealingService,
        IShiftVisibilityGuard shiftVisibilityGuard,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _shiftVisibilityGuard = shiftVisibilityGuard;
        _shiftRepository = shiftRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
        _defaultShiftMacroResolver = defaultShiftMacroResolver;
        _orderSealingService = orderSealingService;
    }

    public async Task<ShiftResource?> Handle(PostCommand<ShiftResource> request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating new shift: Name={Name}, MacroId={MacroId}, ClientId={ClientId}, GroupCount={GroupCount}",
            request.Resource.Name, request.Resource.MacroId, request.Resource.ClientId, request.Resource.Groups?.Count ?? 0);

        var shift = _scheduleMapper.ToShiftEntity(request.Resource);
        GuardCreatableStatus(shift);
        ClearServerOwnedHierarchy(shift);
        await _shiftVisibilityGuard.EnsureGroupChangesVisibleAsync([], shift.GroupItems, cancellationToken);

        if (!shift.MacroId.HasValue || shift.MacroId.Value == Guid.Empty)
        {
            shift.MacroId = await _defaultShiftMacroResolver.ResolveDefaultMacroIdAsync(cancellationToken);
        }

        GuardClientlessRules(shift);

        var resultShift = await _shiftRepository.AddWithSealedOrderHandling(shift);
        await _unitOfWork.CompleteAsync();

        var freshShift = await _shiftRepository.Get(resultShift.Id);

        _logger.LogInformation("Shift created successfully: Id={ShiftId}, Name={Name}, Status={Status}",
            resultShift.Id, resultShift.Name, resultShift.Status);

        return _scheduleMapper.ToShiftResource(freshShift ?? resultShift);
    }

    private static void GuardCreatableStatus(Domain.Models.Schedules.Shift shift)
    {
        if (shift.Status is not (ShiftStatus.OriginalOrder or ShiftStatus.SealedOrder))
        {
            throw new BadRequestException(string.Format(ShiftHierarchyMessages.PostStatusNotAllowed, shift.Status));
        }
    }

    private static void ClearServerOwnedHierarchy(Domain.Models.Schedules.Shift shift)
    {
        shift.OriginalId = null;
        shift.ParentId = null;
        shift.RootId = null;
        shift.Lft = null;
        shift.Rgt = null;
    }

    private void GuardClientlessRules(Domain.Models.Schedules.Shift shift)
    {
        if (ClientlessOrderRules.IsDraftWithoutCustomer(shift))
        {
            throw new BadRequestException(ClientlessOrderRules.DraftWithoutCustomerMessage);
        }

        if (!ClientlessOrderRules.IsSealedWithoutCustomer(shift))
        {
            return;
        }

        var missing = _orderSealingService.CollectMissingRequirements(shift);
        if (missing.Count > 0)
        {
            throw new BadRequestException(ClientlessOrderRules.IncompleteClientlessOrderMessage(missing));
        }
    }
}
