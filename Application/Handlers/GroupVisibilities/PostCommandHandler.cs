using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.GroupVisibilities;
public class PostCommandHandler : IRequestHandler<PostCommand<GroupVisibilityResource>, GroupVisibilityResource?>
{
    private readonly ILogger<PostCommandHandler> logger;
    private readonly IMapper mapper;
    private readonly IGroupVisibilityRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public PostCommandHandler(
                              IMapper mapper,
                              IGroupVisibilityRepository repository,
                              IUnitOfWork unitOfWork,
                              ILogger<PostCommandHandler> logger)
    {
        this.mapper = mapper;
        this.repository = repository;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    public async Task<GroupVisibilityResource?> Handle(PostCommand<GroupVisibilityResource> request, CancellationToken cancellationToken)
    {       
        try
        {
            var groupVisibility = mapper.Map<GroupVisibilityResource, Models.Associations.GroupVisibility>(request.Resource);

            await repository.Add(groupVisibility);

            await unitOfWork.CompleteAsync();
         
            logger.LogInformation("Command {CommandName} processed successfully.", "PostCommand<GroupVisibilityResource>");

            var createdGroupVisibility = await repository.Get(groupVisibility.Id);

            return createdGroupVisibility != null ? mapper.Map<Models.Associations.GroupVisibility, GroupVisibilityResource>(createdGroupVisibility) : null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating groupVisibility: {ErrorMessage}", ex.Message);
   
            throw;
        }
    }
}