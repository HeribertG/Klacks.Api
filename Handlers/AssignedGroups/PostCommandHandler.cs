using AutoMapper;
using Klacks.Api.Commands;
using Klacks.Api.Interfaces;
using Klacks.Api.Models.Staffs;
using Klacks.Api.Resources.Staffs;
using MediatR;

namespace Klacks.Api.Handlers.AssignedGroups;

public class PostCommandHandler : IRequestHandler<PostCommand<AssignedGroupResource>, AssignedGroupResource?>
{
    private readonly ILogger<PostCommandHandler> logger;
    private readonly IMapper mapper;
    private readonly IAssignedGroupRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public PostCommandHandler(
                              IMapper mapper,
                              IAssignedGroupRepository repository,
                              IUnitOfWork unitOfWork,
                              ILogger<PostCommandHandler> logger)
    {
        this.mapper = mapper;
        this.repository = repository;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    public async Task<AssignedGroupResource?> Handle(PostCommand<AssignedGroupResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var assignedGroup = mapper.Map<AssignedGroupResource, AssignedGroup>(request.Resource);

            await repository.Add(assignedGroup);

            await unitOfWork.CompleteAsync();

            logger.LogInformation("New assignedGroup added successfully. ID: {assignedGroup}", assignedGroup.Id);

            return mapper.Map<AssignedGroup, AssignedGroupResource>(assignedGroup);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while adding a new address. ID: {assignedGroup}", request.Resource.Id);
            throw;
        }
    }
}

