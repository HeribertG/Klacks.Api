using AutoMapper;
using Klacks.Api.Commands;
using Klacks.Api.Interfaces;
using Klacks.Api.Resources.Schedules;
using MediatR;

namespace Klacks.Api.Handlers.Shifts;

public class PutCommandHandler : IRequestHandler<PutCommand<ShiftResource>, ShiftResource?>
{
  private readonly ILogger<PutCommandHandler> logger;
  private readonly IMapper mapper;
  private readonly IShiftRepository repository;
  private readonly IUnitOfWork unitOfWork;

  public PutCommandHandler(
                          IMapper mapper,
                          IShiftRepository repository,
                          IUnitOfWork unitOfWork,
                          ILogger<PutCommandHandler> logger)
  {
    this.mapper = mapper;
    this.repository = repository;
    this.unitOfWork = unitOfWork;
    this.logger = logger;
  }

  public async Task<ShiftResource?> Handle(PutCommand<ShiftResource> request, CancellationToken cancellationToken)
  {
    try
    {
      var dbShift = await repository.Get(request.Resource.Id);
      if (dbShift == null)
      {
        logger.LogWarning("Shift with ID {ShiftId} not found.", request.Resource.Id);
        return null;
      }

      var updatedShift = mapper.Map(request.Resource, dbShift);
      updatedShift = repository.Put(updatedShift);
      await unitOfWork.CompleteAsync();

      logger.LogInformation("Shift with ID {ShiftId} updated successfully.", request.Resource.Id);

      return mapper.Map<Models.Schedules.Shift, ShiftResource>(updatedShift);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error occurred while updating shift with ID {ShiftId}.", request.Resource.Id);
      throw;
    }
  }
}
