using AutoMapper;
using Klacks_api.Commands;
using Klacks_api.Interfaces;
using Klacks_api.Resources.Schedules;
using MediatR;

namespace Klacks_api.Handlers.Breaks;

public class PostCommandHandler : IRequestHandler<PostCommand<BreakResource>, BreakResource?>
{
  private readonly ILogger<PostCommandHandler> logger;
  private readonly IMapper mapper;
  private readonly IBreakRepository repository;
  private readonly IUnitOfWork unitOfWork;

  public PostCommandHandler(
                            IMapper mapper,
                            IBreakRepository repository,
                            IUnitOfWork unitOfWork,
                            ILogger<PostCommandHandler> logger)
  {
    this.mapper = mapper;
    this.repository = repository;
    this.unitOfWork = unitOfWork;
    this.logger = logger;
  }

  public async Task<BreakResource?> Handle(PostCommand<BreakResource> request, CancellationToken cancellationToken)
  {
    try
    {
      var breakItem = mapper.Map<BreakResource, Models.Schedules.Break>(request.Resource);
      repository.Add(breakItem);

      await unitOfWork.CompleteAsync();

      logger.LogInformation("New break added successfully. ID: {BreakId}", breakItem.Id);

      return mapper.Map<Models.Schedules.Break, BreakResource>(breakItem);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error occurred while adding a new break. ID: {BreakId}", request.Resource.Id);
      throw;
    }
  }
}
