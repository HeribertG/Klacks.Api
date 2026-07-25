// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for deleting an individual period aggregate. Refuses when an active contract still
/// references it, and cascades the soft-delete to its period rows.
/// </summary>
/// <param name="request">Contains the id of the individual period to delete</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.IndividualPeriods;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<IndividualPeriodResource>, IndividualPeriodResource?>
{
    private readonly IIndividualPeriodRepository _individualPeriodRepository;
    private readonly IContractRepository _contractRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCommandHandler(
        IIndividualPeriodRepository individualPeriodRepository,
        IContractRepository contractRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _individualPeriodRepository = individualPeriodRepository;
        _contractRepository = contractRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<IndividualPeriodResource?> Handle(DeleteCommand<IndividualPeriodResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            _logger.LogInformation("Deleting individual period with ID: {IndividualPeriodId}", request.Id);

            var existingIndividualPeriod = await _individualPeriodRepository.Get(request.Id);
            if (existingIndividualPeriod == null)
            {
                _logger.LogWarning("IndividualPeriod not found: {IndividualPeriodId}", request.Id);
                return null;
            }

            var activeContractCount = await _contractRepository.CountActiveContractsByIndividualPeriodAsync(request.Id);
            if (activeContractCount > 0)
            {
                throw new InvalidRequestException(
                    $"Cannot delete individual period '{existingIndividualPeriod.Name}': it is still referenced by {activeContractCount} active contract(s).");
            }

            var individualPeriodResource = _scheduleMapper.ToIndividualPeriodResource(existingIndividualPeriod);

            await _individualPeriodRepository.Delete(request.Id);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("IndividualPeriod deleted successfully: {IndividualPeriodId}", request.Id);
            return individualPeriodResource;
        },
        "deleting individual period",
        new { request.Id });
    }
}
