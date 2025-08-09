using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Interfaces;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Handlers.States;

public class DeleteCommandHandler : IRequestHandler<DeleteCommand<StateResource>, StateResource?>
{
    private readonly ILogger<DeleteCommandHandler> logger;
    private readonly IMapper mapper;
    private readonly IStateRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public DeleteCommandHandler(
                                IMapper mapper,
                                IStateRepository repository,
                                IUnitOfWork unitOfWork,
                                ILogger<DeleteCommandHandler> logger)
    {
        this.mapper = mapper;
        this.repository = repository;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    public async Task<StateResource?> Handle(DeleteCommand<StateResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var state = await this.repository.Delete(request.Id);
            if (state == null)
            {
                logger.LogWarning("State with ID {StateId} not found for deletion.", request.Id);
                return null;
            }

            await this.unitOfWork.CompleteAsync();

            logger.LogInformation("State with ID {StateId} deleted successfully.", request.Id);

            return this.mapper.Map<Models.Settings.State, StateResource>(state);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while deleting state with ID {StateId}.", request.Id);
            throw;
        }
    }
}
