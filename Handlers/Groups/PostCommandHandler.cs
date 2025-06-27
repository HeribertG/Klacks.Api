using AutoMapper;
using Klacks.Api.Commands;
using Klacks.Api.Interfaces;
using MediatR;

namespace Klacks.Api.Handlers.Groups;

public class PostCommandHandler : IRequestHandler<PostCommand<GroupResource>, GroupResource?>
{
    private readonly ILogger<PostCommandHandler> logger;
    private readonly IMapper mapper;
    private readonly IGroupRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public PostCommandHandler(
                              IMapper mapper,
                              IGroupRepository repository,
                              IUnitOfWork unitOfWork,
                              ILogger<PostCommandHandler> logger)
    {
        this.mapper = mapper;
        this.repository = repository;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    public async Task<GroupResource?> Handle(PostCommand<GroupResource> request, CancellationToken cancellationToken)
    {
        using var transaction = await unitOfWork.BeginTransactionAsync();
        try
        {
            var group = mapper.Map<GroupResource, Models.Associations.Group>(request.Resource);

            await repository.Add(group);

            await unitOfWork.CompleteAsync();
            await unitOfWork.CommitTransactionAsync(transaction);

            logger.LogInformation("Command {CommandName} processed successfully.", "PostCommand<GroupResource>");

            var createdGroup = await repository.Get(group.Id);

            return createdGroup != null ? mapper.Map<Models.Associations.Group, GroupResource>(createdGroup) : null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating group: {ErrorMessage}", ex.Message);
            await unitOfWork.RollbackTransactionAsync(transaction);
            throw;
        }
    }
}