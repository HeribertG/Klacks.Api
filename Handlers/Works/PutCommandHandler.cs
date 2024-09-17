using AutoMapper;
using Klacks_api.Commands;
using Klacks_api.Interfaces;
using Klacks_api.Resources.Schedules;
using MediatR;

namespace Klacks_api.Handlers.Works;

public class PutCommandHandler : IRequestHandler<PutCommand<WorkResource>, WorkResource?>
{
  private readonly ILogger<PutCommandHandler> logger;
  private readonly IMapper mapper;
  private readonly IWorkRepository repository;
  private readonly IUnitOfWork unitOfWork;

  public PutCommandHandler(
                            IMapper mapper,
                            IWorkRepository repository,
                            IUnitOfWork unitOfWork,
                            ILogger<PutCommandHandler> logger)
  {
    this.mapper = mapper;
    this.repository = repository;
    this.unitOfWork = unitOfWork;
    this.logger = logger;
  }

  public async Task<WorkResource?> Handle(PutCommand<WorkResource> request, CancellationToken cancellationToken)
  {
    try
    {
      var dbWork = await repository.Get(request.Resource.Id);
      if (dbWork == null)
      {
        logger.LogWarning("Work with ID {WorkId} not found.", request.Resource.Id);
        return null;
      }

      var updatedWork = mapper.Map(request.Resource, dbWork);
      updatedWork = repository.Put(updatedWork);
      await unitOfWork.CompleteAsync();

      logger.LogInformation("Work with ID {WorkId} updated successfully.", request.Resource.Id);

      return mapper.Map<Models.Schedules.Work, WorkResource>(updatedWork);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error occurred while updating work with ID {WorkId}.", request.Resource.Id);
      throw;
    }
  }
}
