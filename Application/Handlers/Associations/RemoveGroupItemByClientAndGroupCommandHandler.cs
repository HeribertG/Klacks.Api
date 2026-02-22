using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.Associations;

public class RemoveGroupItemByClientAndGroupCommandHandler : IRequestHandler<RemoveGroupItemByClientAndGroupCommand, bool>
{
    private readonly IGroupItemRepository _groupItemRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveGroupItemByClientAndGroupCommandHandler(
        IGroupItemRepository groupItemRepository,
        IUnitOfWork unitOfWork)
    {
        _groupItemRepository = groupItemRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(RemoveGroupItemByClientAndGroupCommand request, CancellationToken cancellationToken)
    {
        var groupItem = await _groupItemRepository.GetByClientAndGroup(request.ClientId, request.GroupId);
        if (groupItem == null) return false;

        _groupItemRepository.Remove(groupItem);
        await _unitOfWork.CompleteAsync();
        return true;
    }
}
