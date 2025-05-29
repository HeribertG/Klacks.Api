using AutoMapper;
using Klacks.Api.Commands;
using Klacks.Api.Interfaces;
using Klacks.Api.Resources.Associations;
using MediatR;

namespace Klacks.Api.Handlers.Groups;

public class DeleteCommandHandler : IRequestHandler<DeleteCommand<GroupResource>, GroupResource?>
{
    private readonly ILogger<DeleteCommandHandler> logger;
    private readonly IMapper mapper;
    private readonly IGroupRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public DeleteCommandHandler(
                                IMapper mapper,
                                IGroupRepository repository,
                                IUnitOfWork unitOfWork,
                                ILogger<DeleteCommandHandler> logger)
    {
        this.mapper = mapper;
        this.repository = repository;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    public async Task<GroupResource?> Handle(DeleteCommand<GroupResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var group = await repository.Delete(request.Id);

            if (group == null)
            {
                logger.LogWarning("Group with ID {GroupId} not found for deletion.", request.Id);
                return null;
            }

            await unitOfWork.CompleteAsync();

            logger.LogInformation("Group with ID {GroupId} deleted successfully.", request.Id);

            return mapper.Map<Models.Associations.Group, GroupResource>(group);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting group with ID {GroupId}.", request.Id);
            throw;
        }
    }
}
