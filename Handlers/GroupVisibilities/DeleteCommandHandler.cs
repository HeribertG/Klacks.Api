using AutoMapper;
using Klacks.Api.Commands;
using Klacks.Api.Interfaces;
using Klacks.Api.Presentation.Resources.Associations;
using MediatR;

namespace Klacks.Api.Handlers.GroupVisibilities;

public class DeleteCommandHandler : IRequestHandler<DeleteCommand<GroupVisibilityResource>, GroupVisibilityResource?>
{
    private readonly ILogger<GroupVisibilityResource> logger;
    private readonly IMapper mapper;
    private readonly IGroupVisibilityRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public DeleteCommandHandler(
                                IMapper mapper,
                                IGroupVisibilityRepository repository,
                                IUnitOfWork unitOfWork,
                                ILogger<GroupVisibilityResource> logger)
    {
        this.mapper = mapper;
        this.repository = repository;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    public async Task<GroupVisibilityResource?> Handle(DeleteCommand<GroupVisibilityResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var groupVisibility = await repository.Delete(request.Id);

            if (groupVisibility == null)
            {
                logger.LogWarning("GroupVisibility with ID {GroupId} not found for deletion.", request.Id);
                return null;
            }

            await unitOfWork.CompleteAsync();

            logger.LogInformation("GroupVisibility with ID {GroupId} deleted successfully.", request.Id);

            return mapper.Map<Models.Associations.GroupVisibility, GroupVisibilityResource>(groupVisibility);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting groupVisibility with ID {GroupId}.", request.Id);
            throw;
        }
    }
}
