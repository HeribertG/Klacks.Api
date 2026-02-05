using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Contracts;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<ContractResource>, ContractResource?>
{
    private readonly IContractRepository _contractRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCommandHandler(
        IContractRepository contractRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _contractRepository = contractRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
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

            var contractResource = _scheduleMapper.ToContractResource(existingContract);
            await _contractRepository.Delete(request.Id);
            await _unitOfWork.CompleteAsync();

            return contractResource;
        }, 
        "deleting contract", 
        new { ContractId = request.Id });
    }
}