using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Interfaces;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Handlers.Clients;

public class PutCommandHandler(
                          IMapper mapper,
                          IClientRepository repository,
                          IUnitOfWork unitOfWork,
                          ILogger<PutCommandHandler> logger) : IRequestHandler<PutCommand<ClientResource>, ClientResource?>
{
    private readonly ILogger<PutCommandHandler> logger = logger;
    private readonly IMapper mapper = mapper;
    private readonly IClientRepository repository = repository;
    private readonly IUnitOfWork unitOfWork = unitOfWork;

    public async Task<ClientResource?> Handle(PutCommand<ClientResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var dbClient = await repository.Get(request.Resource.Id);
            if (dbClient == null)
            {
                logger.LogWarning("Client with ID {ClientId} not found.", request.Resource.Id);
                return null;
            }

            var updatedClient = mapper.Map(request.Resource, dbClient);
            updatedClient = await repository.Put(updatedClient);
            await unitOfWork.CompleteAsync();

            logger.LogInformation("Client with ID {ClientId} updated successfully.", request.Resource.Id);

            return mapper.Map<Models.Staffs.Client, ClientResource>(updatedClient);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while updating client with ID {ClientId}.", request.Resource.Id);
            throw;
        }
    }
}
