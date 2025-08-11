using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.Contracts;

public class DeleteCommandHandler : IRequestHandler<DeleteCommand<ContractResource>, ContractResource?>
{
    private readonly ILogger<DeleteCommandHandler> logger;
    private readonly IMapper mapper;
    private readonly IContractRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public DeleteCommandHandler(
                                IMapper mapper,
                                IContractRepository repository,
                                IUnitOfWork unitOfWork,
                                ILogger<DeleteCommandHandler> logger)
    {
        this.mapper = mapper;
        this.repository = repository;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    public async Task<ContractResource?> Handle(DeleteCommand<ContractResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var contract = await repository.Delete(request.Id);
            if (contract == null)
            {
                logger.LogWarning("Contract with ID {ContractId} not found for deletion.", request.Id);
                return null;
            }

            await unitOfWork.CompleteAsync();

            logger.LogInformation("Contract with ID {ContractId} deleted successfully.", request.Id);

            return mapper.Map<Klacks.Api.Domain.Models.Associations.Contract, ContractResource>(contract);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while deleting contract with ID {ContractId}.", request.Id);
            throw;
        }
    }
}