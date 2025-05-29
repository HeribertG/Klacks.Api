using AutoMapper;
using Klacks.Api.Commands;
using Klacks.Api.Interfaces;
using Klacks.Api.Resources.Staffs;
using MediatR;

namespace Klacks.Api.Handlers.Clients;

public class PostCommandHandler : IRequestHandler<PostCommand<ClientResource>, ClientResource?>
{
    private readonly ILogger<PostCommandHandler> logger;
    private readonly IMapper mapper;
    private readonly IClientRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public PostCommandHandler(
                              IMapper mapper,
                              IClientRepository repository,
                              IUnitOfWork unitOfWork,
                              ILogger<PostCommandHandler> logger)
    {
        this.mapper = mapper;
        this.repository = repository;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    public async Task<ClientResource?> Handle(PostCommand<ClientResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var client = mapper.Map<ClientResource, Models.Staffs.Client>(request.Resource);

            await repository.Add(client);

            await unitOfWork.CompleteAsync();

            logger.LogInformation("New client added successfully. ID: {ClientId}", client.Id);

            return mapper.Map<Models.Staffs.Client, ClientResource>(client);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while adding a new client.");
            throw;
        }
    }
}
