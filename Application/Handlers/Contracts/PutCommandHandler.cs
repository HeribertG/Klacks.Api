using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.Contracts;

public class PutCommandHandler : IRequestHandler<PutCommand<ContractResource>, ContractResource?>
{
    private readonly ILogger<PutCommandHandler> logger;
    private readonly IMapper mapper;
    private readonly IContractRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public PutCommandHandler(
                             IMapper mapper,
                             IContractRepository repository,
                             IUnitOfWork unitOfWork,
                             ILogger<PutCommandHandler> logger)
    {
        this.mapper = mapper;
        this.repository = repository;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    public async Task<ContractResource?> Handle(PutCommand<ContractResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var dbContract = await repository.Get(request.Resource.Id);
            if (dbContract == null)
            {
                logger.LogWarning("Contract with ID {ContractId} not found.", request.Resource.Id);
                return null;
            }

            var updatedContract = mapper.Map(request.Resource, dbContract);
            updatedContract = await repository.Put(updatedContract);
            await unitOfWork.CompleteAsync();

            logger.LogInformation("Contract with ID {ContractId} updated successfully.", request.Resource.Id);

            return mapper.Map<Klacks.Api.Domain.Models.Associations.Contract, ContractResource>(updatedContract);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while updating contract with ID {ContractId}.", request.Resource.Id);
            throw;
        }
    }
}