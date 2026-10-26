// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Settings;

namespace Klacks.Api.Application.Handlers.Contracts;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<ContractResource>, ContractResource?>
{
    private const string ContractInUseMessageFormat =
        "Contract '{0}' is still assigned to {1} employee(s) with an active or future assignment and cannot be deleted. End or remove those assignments first.";

    private readonly IContractRepository _contractRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICompanyClock _companyClock;

    public DeleteCommandHandler(
        IContractRepository contractRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        ICompanyClock companyClock,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _contractRepository = contractRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
        _companyClock = companyClock;
    }

    public async Task<ContractResource?> Handle(DeleteCommand<ContractResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var existingContract = await _contractRepository.Get(request.Id);
            if (existingContract == null)
            {
                throw new KeyNotFoundException($"Contract with ID {request.Id} not found.");
            }

            var today = DateOnly.FromDateTime(await _companyClock.GetTodayAsync(cancellationToken));
            var assignmentCount = await _contractRepository.CountActiveOrFutureAssignmentsAsync(request.Id, today);
            if (assignmentCount > 0)
            {
                throw new ConflictException(string.Format(ContractInUseMessageFormat, existingContract.Name, assignmentCount));
            }

            var contractResource = _scheduleMapper.ToContractResource(existingContract);
            await _contractRepository.Delete(request.Id);
            await _unitOfWork.CompleteAsync();

            return contractResource;
        }, 
        "deleting contract", 
        new { ContractId = request.Id });
    }
}