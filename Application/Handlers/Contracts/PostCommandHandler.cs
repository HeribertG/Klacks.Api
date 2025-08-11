using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.Contracts;

public class PostCommandHandler : IRequestHandler<PostCommand<ContractResource>, ContractResource?>
{
    private readonly ILogger<PostCommandHandler> logger;
    private readonly IMapper mapper;
    private readonly IContractRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public PostCommandHandler(
                              IMapper mapper,
                              IContractRepository repository,
                              IUnitOfWork unitOfWork,
                              ILogger<PostCommandHandler> logger)
    {
        this.mapper = mapper;
        this.repository = repository;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    public async Task<ContractResource?> Handle(PostCommand<ContractResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var contract = mapper.Map<ContractResource, Klacks.Api.Domain.Models.Associations.Contract>(request.Resource);

            await repository.Add(contract);

            await unitOfWork.CompleteAsync();

            logger.LogInformation("New contract added successfully. ID: {ContractId}", contract.Id);

            return mapper.Map<Klacks.Api.Domain.Models.Associations.Contract, ContractResource>(contract);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while adding a new contract.");
            throw;
        }
    }
}