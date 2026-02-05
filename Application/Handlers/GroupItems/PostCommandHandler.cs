using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.GroupItems;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<GroupItemResource>, GroupItemResource?>
{
    private readonly IGroupItemRepository _groupItemRepository;
    private readonly GroupMapper _groupMapper;
    private readonly IUnitOfWork _unitOfWork;

    public PostCommandHandler(
        IGroupItemRepository groupItemRepository,
        GroupMapper groupMapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _groupItemRepository = groupItemRepository;
        _groupMapper = groupMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<GroupItemResource?> Handle(PostCommand<GroupItemResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var groupItem = _groupMapper.ToGroupItemEntity(request.Resource);
            await _groupItemRepository.Add(groupItem);
            await _unitOfWork.CompleteAsync();
            return _groupMapper.ToGroupItemResource(groupItem);
        },
        "creating group item",
        new { });
    }
}
