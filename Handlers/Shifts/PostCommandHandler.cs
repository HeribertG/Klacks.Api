using AutoMapper;
using Klacks.Api.Commands;
using Klacks.Api.Interfaces;
using Klacks.Api.Resources.Schedules;
using MediatR;

namespace Klacks.Api.Handlers.Shifts;

public class PostCommandHandler : IRequestHandler<PostCommand<ShiftResource>, ShiftResource?>
{
  private readonly ILogger<PostCommandHandler> logger;
  private readonly IMapper mapper;
  private readonly IShiftRepository repository;
  private readonly IUnitOfWork unitOfWork;

  public PostCommandHandler(
                          IMapper mapper,
                          IShiftRepository repository,
                          IUnitOfWork unitOfWork,
                          ILogger<PostCommandHandler> logger)
  {
    this.mapper = mapper;
    this.repository = repository;
    this.unitOfWork = unitOfWork;
    this.logger = logger;
  }

  public async Task<ShiftResource?> Handle(PostCommand<ShiftResource> request, CancellationToken cancellationToken)
  {
    try
    {
      var shift = mapper.Map<ShiftResource, Models.Schedules.Shift>(request.Resource);

      repository.Add(shift);

      await unitOfWork.CompleteAsync();

      logger.LogInformation("New shift added successfully. ID: {ShiftId}", shift.Id);

      return mapper.Map<Models.Schedules.Shift, ShiftResource>(shift);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error occurred while adding a new shift.");
      throw;
    }
  }
}
