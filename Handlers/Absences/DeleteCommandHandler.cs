using AutoMapper;
using Klacks_api.Commands;
using Klacks_api.Interfaces;
using Klacks_api.Resources.Schedules;
using MediatR;

namespace Klacks_api.Handlers.Absences;

public class DeleteCommandHandler : IRequestHandler<DeleteCommand<AbsenceResource>, AbsenceResource?>
{
  private readonly ILogger<DeleteCommandHandler> logger;
  private readonly IMapper mapper;
  private readonly IAbsenceRepository repository;
  private readonly IUnitOfWork unitOfWork;

  public DeleteCommandHandler(
                              IMapper mapper,
                              IAbsenceRepository repository,
                              IUnitOfWork unitOfWork,
                              ILogger<DeleteCommandHandler> logger)
  {
    this.mapper = mapper;
    this.repository = repository;
    this.unitOfWork = unitOfWork;
    this.logger = logger;
  }

  public async Task<AbsenceResource?> Handle(DeleteCommand<AbsenceResource> request, CancellationToken cancellationToken)
  {
    try
    {
      var absence = await repository.Delete(request.Id);
      if (absence == null)
      {
        logger.LogWarning("Absence with ID {AbsenceId} not found for deletion.", request.Id);
        return null;
      }

      await unitOfWork.CompleteAsync();

      logger.LogInformation("Absence with ID {AbsenceId} deleted successfully.", request.Id);

      return mapper.Map<Models.Schedules.Absence, AbsenceResource>(absence);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error occurred while deleting absence with ID {AbsenceId}.", request.Id);
      throw;
    }
  }
}
