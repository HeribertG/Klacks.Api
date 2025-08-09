using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.GroupVisibilities;

public class PutCommandHandler : IRequestHandler<PutCommand<GroupVisibilityResource>, GroupVisibilityResource?>
{
    private readonly ILogger<PutCommandHandler> logger;
    private readonly IMapper mapper;
    private readonly IGroupVisibilityRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public PutCommandHandler(
                              IMapper mapper,
                              IGroupVisibilityRepository repository,
                              IUnitOfWork unitOfWork,
                              ILogger<PutCommandHandler> logger)
    {
        this.mapper = mapper;
        this.repository = repository;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    public async Task<GroupVisibilityResource?> Handle(PutCommand<GroupVisibilityResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var dbGroupVisibility = await repository.Get(request.Resource.Id);
            if (dbGroupVisibility == null)
            {
                logger.LogWarning("GroupVisibility with ID {GroupId} not found.", request.Resource.Id);
                return null;
            }

            var updatedGroupVisibility = mapper.Map(request.Resource, dbGroupVisibility);

            await repository.Put(updatedGroupVisibility);
            await unitOfWork.CompleteAsync();

            var dbUpdatedGroup = await repository.Get(request.Resource.Id);

            logger.LogInformation("Group with ID {GroupId} updated successfully.", request.Resource.Id);

            return dbUpdatedGroup != null ? mapper.Map<Models.Associations.GroupVisibility, GroupVisibilityResource>(dbUpdatedGroup) : null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating groupVisibility with ID {GroupId}.", request.Resource.Id);
            throw;
        }
    }
}
