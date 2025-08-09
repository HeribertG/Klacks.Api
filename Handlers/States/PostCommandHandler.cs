using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Interfaces;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Handlers.States;

public class PostCommandHandler : IRequestHandler<PostCommand<StateResource>, StateResource?>
{
    private readonly ILogger<PostCommandHandler> logger;
    private readonly IMapper mapper;
    private readonly IStateRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public PostCommandHandler(
                              IMapper mapper,
                              IStateRepository repository,
                              IUnitOfWork unitOfWork,
                              ILogger<PostCommandHandler> logger)
    {
        this.mapper = mapper;
        this.repository = repository;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    public async Task<StateResource?> Handle(PostCommand<StateResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var state = this.mapper.Map<StateResource, Models.Settings.State>(request.Resource);
            await this.repository.Add(state);

            await this.unitOfWork.CompleteAsync();

            logger.LogInformation("New state added successfully. ID: {StateId}", state.Id);

            return this.mapper.Map<Models.Settings.State, StateResource>(state);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while adding a new state.");
            throw;
        }
    }
}
