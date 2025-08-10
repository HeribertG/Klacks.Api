using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.Breaks;

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
            var breakItem = mapper.Map<BreakResource, Klacks.Api.Domain.Models.Schedules.Break>(request.Resource);
            await repository.Add(breakItem);

            await unitOfWork.CompleteAsync();

            logger.LogInformation("New break added successfully. ID: {BreakId}", breakItem.Id);

            return mapper.Map<Klacks.Api.Domain.Models.Schedules.Break, BreakResource>(breakItem);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while adding a new break. ID: {BreakId}", request.Resource.Id);
            throw;
        }
    }
}
