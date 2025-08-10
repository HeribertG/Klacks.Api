using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.Communications;

public class PutCommandHandler : IRequestHandler<PutCommand<CommunicationResource>, CommunicationResource?>
{
    private readonly ILogger<PutCommandHandler> logger;
    private readonly IMapper mapper;
    private readonly ICommunicationRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public PutCommandHandler(
                              IMapper mapper,
                              ICommunicationRepository repository,
                              IUnitOfWork unitOfWork,
                              ILogger<PutCommandHandler> logger)
    {
        this.mapper = mapper;
        this.repository = repository;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    public async Task<CommunicationResource?> Handle(PutCommand<CommunicationResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var dbCommunication = await repository.Get(request.Resource.Id);
            if (dbCommunication == null)
            {
                logger.LogWarning("Communication with ID {CommunicationId} not found.", request.Resource.Id);
                return null;
            }

            var updatedCommunication = mapper.Map(request.Resource, dbCommunication);
            updatedCommunication = await repository.Put(updatedCommunication);
            await unitOfWork.CompleteAsync();

            logger.LogInformation("Communication with ID {CommunicationId} updated successfully.", request.Resource.Id);

            return mapper.Map<Klacks.Api.Domain.Models.Staffs.Communication, CommunicationResource>(updatedCommunication);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while updating communication with ID {CommunicationId}.", request.Resource.Id);
            throw;
        }
    }
}
