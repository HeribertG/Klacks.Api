using AutoMapper;
using Klacks.Api.Commands;
using Klacks.Api.Interfaces;
using Klacks.Api.Resources.Settings;
using MediatR;

namespace Klacks.Api.Handlers.Communications;

public class PostCommandHandler : IRequestHandler<PostCommand<CommunicationResource>, CommunicationResource?>
{
    private readonly ILogger<PostCommandHandler> logger;
    private readonly IMapper mapper;
    private readonly ICommunicationRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public PostCommandHandler(
                              IMapper mapper,
                              ICommunicationRepository repository,
                              IUnitOfWork unitOfWork,
                              ILogger<PostCommandHandler> logger)
    {
        this.mapper = mapper;
        this.repository = repository;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    public async Task<CommunicationResource?> Handle(PostCommand<CommunicationResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var communication = mapper.Map<CommunicationResource, Models.Staffs.Communication>(request.Resource);

            await repository.Add(communication);

            await unitOfWork.CompleteAsync();

            logger.LogInformation("New communication added successfully. ID: {CommunicationId}", communication.Id);

            return mapper.Map<Models.Staffs.Communication, CommunicationResource>(communication);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while adding a new communication.");
            throw;
        }
    }
}
