// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.Contracts;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<ContractResource>, ContractResource?>
{
    private readonly IContractRepository _contractRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;

    public PostCommandHandler(
        IContractRepository contractRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _contractRepository = contractRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<ContractResource?> Handle(PostCommand<ContractResource> request, CancellationToken cancellationToken)
    {
        ValidateContractRequest(request.Resource);

        return await ExecuteAsync(async () =>
        {
            var contract = _scheduleMapper.ToContractEntity(request.Resource);
            await _contractRepository.Add(contract);
            await _unitOfWork.CompleteAsync();
            return _scheduleMapper.ToContractResource(contract);
        }, 
        "creating contract", 
        new { ContractName = request.Resource?.Name });
    }

    private void ValidateContractRequest(ContractResource? resource)
    {
        if (resource == null)
        {
            throw new InvalidRequestException("Contract data is required.");
        }

        if (string.IsNullOrWhiteSpace(resource.Name))
        {
            throw new InvalidRequestException("Contract name is required.");
        }

        if (resource.MinimumHours > resource.MaximumHours)
        {
            throw new InvalidRequestException("Minimum hours cannot exceed maximum hours.");
        }

        if (resource.GuaranteedHours < resource.MinimumHours ||
            resource.GuaranteedHours > resource.MaximumHours)
        {
            throw new InvalidRequestException("Guaranteed hours must be between minimum and maximum hours.");
        }

        if (resource.ValidUntil.HasValue && resource.ValidUntil <= resource.ValidFrom)
        {
            throw new InvalidRequestException("Valid until date must be after valid from date.");
        }
    }
}