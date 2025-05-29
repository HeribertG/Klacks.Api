using AutoMapper;
using Klacks.Api.Commands;
using Klacks.Api.Interfaces;
using Klacks.Api.Resources.Schedules;
using MediatR;

namespace Klacks.Api.Handlers.Breaks;

public class PutCommandHandler : IRequestHandler<PutCommand<BreakResource>, BreakResource?>
{
    private readonly ILogger<PutCommandHandler> logger;
    private readonly IMapper mapper;
    private readonly IBreakRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public PutCommandHandler(
                              IMapper mapper,
                              IBreakRepository repository,
                              IUnitOfWork unitOfWork,
                              ILogger<PutCommandHandler> logger)
    {
        this.mapper = mapper;
        this.repository = repository;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    public async Task<BreakResource?> Handle(PutCommand<BreakResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var dbBreak = await repository.Get(request.Resource.Id);
            if (dbBreak == null)
            {
                logger.LogWarning("Break with ID {BreakId} not found.", request.Resource.Id);
                return null;
            }

            var updatedBreak = mapper.Map(request.Resource, dbBreak);
            updatedBreak = await repository.Put(updatedBreak);
            await unitOfWork.CompleteAsync();

            logger.LogInformation("Break with ID {BreakId} updated successfully.", request.Resource.Id);

            return mapper.Map<Models.Schedules.Break, BreakResource>(updatedBreak);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while updating break with ID {BreakId}.", request.Resource.Id);
            throw;
        }
    }
}
