using AutoMapper;
using Klacks.Api.Commands;
using Klacks.Api.Interfaces;
using Klacks.Api.Resources.Schedules;
using MediatR;

namespace Klacks.Api.Handlers.Breaks;

public class DeleteCommandHandler : IRequestHandler<DeleteCommand<BreakResource>, BreakResource?>
{
    private readonly ILogger<DeleteCommandHandler> logger;
    private readonly IMapper mapper;
    private readonly IBreakRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public DeleteCommandHandler(
                                IMapper mapper,
                                IBreakRepository repository,
                                IUnitOfWork unitOfWork,
                                ILogger<DeleteCommandHandler> logger)
    {
        this.mapper = mapper;
        this.repository = repository;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    public async Task<BreakResource?> Handle(DeleteCommand<BreakResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var breakItem = await repository.Delete(request.Id);
            if (breakItem == null)
            {
                logger.LogWarning("Break with ID {BreakId} not found for deletion.", request.Id);
                return null;
            }

            await unitOfWork.CompleteAsync();

            logger.LogInformation("Break with ID {BreakId} deleted successfully.", request.Id);

            return mapper.Map<Models.Schedules.Break, BreakResource>(breakItem);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while deleting break with ID {BreakId}.", request.Id);
            throw;
        }
    }
}
