using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.GroupItems;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<GroupItemResource>, GroupItemResource?>
{
    private readonly IGroupItemRepository _groupItemRepository;
    private readonly GroupMapper _groupMapper;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCommandHandler(
        IGroupItemRepository groupItemRepository,
        GroupMapper groupMapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _groupItemRepository = groupItemRepository;
        _groupMapper = groupMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<GroupItemResource?> Handle(DeleteCommand<GroupItemResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var groupItem = await _groupItemRepository.Delete(request.Id);
            await _unitOfWork.CompleteAsync();
            return _groupMapper.ToGroupItemResource(groupItem);
        },
        "deleting group item",
        new { Id = request.Id });
    }
}
