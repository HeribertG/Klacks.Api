using AutoMapper;
using Klacks.Api.Commands;
using Klacks.Api.Interfaces;
using Klacks.Api.Resources.Settings;
using MediatR;

namespace Klacks.Api.Handlers.Communications;

public class DeleteCommandHandler : IRequestHandler<DeleteCommand<CommunicationResource>, CommunicationResource?>
{
    private readonly ILogger<DeleteCommandHandler> logger;
    private readonly IMapper mapper;
    private readonly ICommunicationRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public DeleteCommandHandler(
                                IMapper mapper,
                                ICommunicationRepository repository,
                                IUnitOfWork unitOfWork,
                                ILogger<DeleteCommandHandler> logger)
    {
        this.mapper = mapper;
        this.repository = repository;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    public async Task<CommunicationResource?> Handle(DeleteCommand<CommunicationResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var communication = await repository.Delete(request.Id);
            if (communication == null)
            {
                logger.LogWarning("Communication with ID {CommunicationId} not found for deletion.", request.Id);
                return null;
            }

            await unitOfWork.CompleteAsync();

            logger.LogInformation("Communication with ID {CommunicationId} deleted successfully.", request.Id);

            return mapper.Map<Models.Staffs.Communication, CommunicationResource>(communication);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while deleting communication with ID {CommunicationId}.", request.Id);
            throw;
        }
    }
}
