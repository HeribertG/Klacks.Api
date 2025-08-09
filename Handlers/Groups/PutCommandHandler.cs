using AutoMapper;
using Klacks.Api.Commands;
using Klacks.Api.Interfaces;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Handlers.Groups;

public class PutCommandHandler : IRequestHandler<PutCommand<GroupResource>, GroupResource?>
{
    private readonly ILogger<PutCommandHandler> logger;
    private readonly IMapper mapper;
    private readonly IGroupRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public PutCommandHandler(
                              IMapper mapper,
                              IGroupRepository repository,
                              IUnitOfWork unitOfWork,
                              ILogger<PutCommandHandler> logger)
    {
        this.mapper = mapper;
        this.repository = repository;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    public async Task<GroupResource?> Handle(PutCommand<GroupResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var dbGroup = await repository.Get(request.Resource.Id);
            if (dbGroup == null)
            {
                logger.LogWarning("Group with ID {GroupId} not found.", request.Resource.Id);
                return null;
            }

            var updatedGroup = mapper.Map(request.Resource, dbGroup);

            await repository.Put(updatedGroup);
            await unitOfWork.CompleteAsync();

            var dbUpdatedGroup = await repository.Get(request.Resource.Id);

            logger.LogInformation("Group with ID {GroupId} updated successfully.", request.Resource.Id);

            return dbUpdatedGroup != null ? mapper.Map<Models.Associations.Group, GroupResource>(dbUpdatedGroup) : null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating group with ID {GroupId}.", request.Resource.Id);
            throw;
        }
    }
}
