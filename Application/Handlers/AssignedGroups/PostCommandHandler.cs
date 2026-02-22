using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.AssignedGroups;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<AssignedGroupResource>, AssignedGroupResource?>
{
    private readonly IAssignedGroupRepository _assignedGroupRepository;
    private readonly GroupMapper _groupMapper;
    private readonly IUnitOfWork _unitOfWork;
    
    public PostCommandHandler(
        IAssignedGroupRepository assignedGroupRepository,
        GroupMapper groupMapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _assignedGroupRepository = assignedGroupRepository;
        _groupMapper = groupMapper;
        _unitOfWork = unitOfWork;
        }

    public async Task<AssignedGroupResource?> Handle(PostCommand<AssignedGroupResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var assignedGroup = _groupMapper.ToAssignedGroupEntity(request.Resource);
            await _assignedGroupRepository.Add(assignedGroup);
            await _unitOfWork.CompleteAsync();
            return _groupMapper.ToAssignedGroupResource(assignedGroup);
        }, 
        "creating group", 
        new { });
    }
}
