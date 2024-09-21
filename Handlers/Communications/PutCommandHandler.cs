using AutoMapper;
using Klacks.Api.Commands;
using Klacks.Api.Interfaces;
using Klacks.Api.Resources.Settings;
using MediatR;

namespace Klacks.Api.Handlers.Communications;

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
      updatedCommunication = repository.Put(updatedCommunication);
      await unitOfWork.CompleteAsync();

      logger.LogInformation("Communication with ID {CommunicationId} updated successfully.", request.Resource.Id);

      return mapper.Map<Models.Staffs.Communication, CommunicationResource>(updatedCommunication);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error occurred while updating communication with ID {CommunicationId}.", request.Resource.Id);
      throw;
    }
  }
}
