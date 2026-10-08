// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Shifts;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<ShiftResource>, ShiftResource?>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IShiftRequiredQualificationRepository _requirementRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;

    public PutCommandHandler(
        IShiftRepository shiftRepository,
        IShiftRequiredQualificationRepository requirementRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _shiftRepository = shiftRepository;
        _requirementRepository = requirementRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<ShiftResource?> Handle(PutCommand<ShiftResource> request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating shift: Id={ShiftId}, Name={Name}, MacroId={MacroId}, ClientId={ClientId}, GroupCount={GroupCount}",
            request.Resource.Id, request.Resource.Name, request.Resource.MacroId, request.Resource.ClientId, request.Resource.Groups?.Count ?? 0);

        var shift = _scheduleMapper.ToShiftEntity(request.Resource);
        await KeepInheritedRequirementsAsync(shift, cancellationToken);

        var resultShift = await _shiftRepository.PutWithSealedOrderHandling(shift);

        if (resultShift == null)
        {
            _logger.LogWarning("Shift not found for update: Id={ShiftId}", request.Resource.Id);
            return null;
        }

        await _unitOfWork.CompleteAsync();

        var freshShift = await _shiftRepository.Get(resultShift.Id);

        _logger.LogInformation("Shift updated successfully: Id={ShiftId}, Name={Name}, Status={Status}",
            resultShift.Id, resultShift.Name, resultShift.Status);

        return _scheduleMapper.ToShiftResource(freshShift ?? resultShift);
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
