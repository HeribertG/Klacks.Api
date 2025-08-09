using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.Works;

public class PostCommandHandler : IRequestHandler<PostCommand<WorkResource>, WorkResource?>
{
    private readonly ILogger<PostCommandHandler> logger;
    private readonly IMapper mapper;
    private readonly IWorkRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public PostCommandHandler(
                              IMapper mapper,
                              IWorkRepository repository,
                              IUnitOfWork unitOfWork,
                              ILogger<PostCommandHandler> logger)
    {
        this.mapper = mapper;
        this.repository = repository;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    public async Task<WorkResource?> Handle(PostCommand<WorkResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var work = mapper.Map<WorkResource, Models.Schedules.Work>(request.Resource);

            await repository.Add(work);

            await unitOfWork.CompleteAsync();

            logger.LogInformation("Work item added successfully. ID: {WorkId}", work.Id);

            return mapper.Map<Models.Schedules.Work, WorkResource>(work);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while adding a new work item.");
            throw;
        }
    }
}
