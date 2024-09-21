using AutoMapper;
using Klacks.Api.Commands;
using Klacks.Api.Interfaces;
using Klacks.Api.Resources.Associations;
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
    var group = mapper.Map<GroupResource, Models.Associations.Group>(request.Resource);

    repository.Add(group);

    await unitOfWork.CompleteAsync();

    logger.LogInformation("Command {CommandName} processed successfully.", "PostCommand<GroupResource>");

    return mapper.Map<Models.Associations.Group, GroupResource>(group);
  }
}
