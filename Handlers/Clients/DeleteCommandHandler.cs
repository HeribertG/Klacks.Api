using AutoMapper;
using Klacks.Api.Commands;
using Klacks.Api.Interfaces;
using Klacks.Api.Resources.Staffs;
using MediatR;

namespace Klacks.Api.Handlers.Clients;

public class DeleteCommandHandler : IRequestHandler<DeleteCommand<ClientResource>, ClientResource?>
{
    private readonly ILogger<DeleteCommandHandler> logger;
    private readonly IMapper mapper;
    private readonly IClientRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public DeleteCommandHandler(
                                IMapper mapper,
                                IClientRepository repository,
                                IUnitOfWork unitOfWork,
                                ILogger<DeleteCommandHandler> logger)
    {
        this.mapper = mapper;
        this.repository = repository;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    public async Task<ClientResource?> Handle(DeleteCommand<ClientResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var client = await repository.Delete(request.Id);
            if (client == null)
            {
                logger.LogWarning("Client with ID {ClientId} not found for deletion.", request.Id);
                return null;
            }

            await unitOfWork.CompleteAsync();

            logger.LogInformation("Client with ID {ClientId} deleted successfully.", request.Id);

            return mapper.Map<Models.Staffs.Client, ClientResource>(client);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while deleting client with ID {ClientId}.", request.Id);
            throw;
        }
    }
}
