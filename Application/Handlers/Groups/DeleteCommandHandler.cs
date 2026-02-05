using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Groups;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<GroupResource>, GroupResource?>
{
    private readonly IGroupRepository _groupRepository;
    private readonly GroupMapper _groupMapper;
    private readonly IUnitOfWork _unitOfWork;
    
    public DeleteCommandHandler(
        IGroupRepository groupRepository,
        GroupMapper groupMapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _groupRepository = groupRepository;
        _groupMapper = groupMapper;
        _unitOfWork = unitOfWork;
        }

    public async Task<GroupResource?> Handle(DeleteCommand<GroupResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var group = await _groupRepository.Get(request.Id);
            if (group == null)
            {
                throw new KeyNotFoundException($"Group with ID {request.Id} not found.");
            }

            var groupToDelete = _groupMapper.ToGroupResource(group);
            await _groupRepository.Delete(request.Id);

            await _unitOfWork.CompleteAsync();

            return groupToDelete;
        }, 
        "deleting group", 
        new { GroupId = request.Id });
    }
}
