using AutoMapper;
using Klacks.Api.Commands;
using Klacks.Api.Interfaces;
using Klacks.Api.Resources.Settings;
using MediatR;

namespace Klacks.Api.Handlers.States;

public class PutCommandHandler : IRequestHandler<PutCommand<StateResource>, StateResource?>
{
  private readonly ILogger<PutCommandHandler> logger;
  private readonly IMapper mapper;
  private readonly IStateRepository repository;
  private readonly IUnitOfWork unitOfWork;

  public PutCommandHandler(IMapper mapper,
                           IStateRepository repository,
                           IUnitOfWork unitOfWork,
                           ILogger<PutCommandHandler> logger)
  {
    this.mapper = mapper;
    this.repository = repository;
    this.unitOfWork = unitOfWork;
    this.logger = logger;
  }

  public async Task<StateResource?> Handle(PutCommand<StateResource> request, CancellationToken cancellationToken)
  {
    try
    {
      var dbState = await this.repository.Get(request.Resource.Id);
      if (dbState == null)
      {
        logger.LogWarning("State with ID {StateId} not found.", request.Resource.Id);
        return null;
      }

      var updatedState = this.mapper.Map(request.Resource, dbState);
      updatedState = this.repository.Put(updatedState);
      await this.unitOfWork.CompleteAsync();

      logger.LogInformation("State with ID {StateId} updated successfully.", request.Resource.Id);

      return this.mapper.Map<Models.Settings.State, StateResource>(updatedState);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error occurred while updating state with ID {StateId}.", request.Resource.Id);
      throw;
    }
  }
}
