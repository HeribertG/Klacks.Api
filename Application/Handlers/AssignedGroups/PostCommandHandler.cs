using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Application.Handlers.AssignedGroups;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<AssignedGroupResource>, AssignedGroupResource?>
{
    private readonly IAssignedGroupRepository _assignedGroupRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    
    public PostCommandHandler(
        IAssignedGroupRepository assignedGroupRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _assignedGroupRepository = assignedGroupRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        }

    public async Task<AssignedGroupResource?> Handle(PostCommand<AssignedGroupResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var assignedGroup = _mapper.Map<AssignedGroup>(request.Resource);
            await _assignedGroupRepository.Add(assignedGroup);
            await _unitOfWork.CompleteAsync();
            return _mapper.Map<AssignedGroupResource>(assignedGroup);
        }, 
        "creating group", 
        new { });
    }
}
