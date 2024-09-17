using AutoMapper;
using Klacks_api.Commands;
using Klacks_api.Interfaces;
using Klacks_api.Resources.Schedules;
using MediatR;

namespace Klacks_api.Handlers.Absences;

public class PostCommandHandler : IRequestHandler<PostCommand<AbsenceResource>, AbsenceResource?>
{
  private readonly ILogger<PostCommandHandler> logger;
  private readonly IMapper mapper;
  private readonly IAbsenceRepository repository;
  private readonly IUnitOfWork unitOfWork;

  public PostCommandHandler(
                            IMapper mapper,
                            IAbsenceRepository repository,
                            IUnitOfWork unitOfWork,
                            ILogger<PostCommandHandler> logger)
  {
    this.mapper = mapper;
    this.repository = repository;
    this.unitOfWork = unitOfWork;
    this.logger = logger;
  }

  public async Task<AbsenceResource?> Handle(PostCommand<AbsenceResource> request, CancellationToken cancellationToken)
  {
    try
    {
      var absence = mapper.Map<AbsenceResource, Models.Schedules.Absence>(request.Resource);
      repository.Add(absence);

      await unitOfWork.CompleteAsync();

      logger.LogInformation("New absence added successfully. ID: {AbsenceId}", absence.Id);

      return mapper.Map<Models.Schedules.Absence, AbsenceResource>(absence);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error occurred while adding a new absence. ID: {AbsenceId}", request.Resource.Id);
      throw;
    }
  }
}
