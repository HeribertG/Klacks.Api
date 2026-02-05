using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Groups;

public class PostCommandHandler : BaseTransactionHandler, IRequestHandler<PostCommand<GroupResource>, GroupResource?>
{
    private readonly IGroupRepository _groupRepository;
    private readonly GroupMapper _groupMapper;
    
    public PostCommandHandler(
        IGroupRepository groupRepository,
        GroupMapper groupMapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
        : base(unitOfWork, logger)
    {
        _groupRepository = groupRepository;
        _groupMapper = groupMapper;
    }

    public async Task<GroupResource?> Handle(PostCommand<GroupResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteWithTransactionAsync(async () =>
        {
            var group = _groupMapper.ToGroupEntity(request.Resource);
            group.Id = Guid.NewGuid();
            await _groupRepository.Add(group);
            await _unitOfWork.CompleteAsync();
            return _groupMapper.ToGroupResource(group);
        },
        "creating group",
        new { GroupId = request.Resource?.Id });
    }
}