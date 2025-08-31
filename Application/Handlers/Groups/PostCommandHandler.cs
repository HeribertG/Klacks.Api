using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.Groups;

public class PostCommandHandler : BaseTransactionHandler, IRequestHandler<PostCommand<GroupResource>, GroupResource?>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IMapper _mapper;
    
    public PostCommandHandler(
        IGroupRepository groupRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
        : base(unitOfWork, logger)
    {
        _groupRepository = groupRepository;
        _mapper = mapper;
    }

    public async Task<GroupResource?> Handle(PostCommand<GroupResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteWithTransactionAsync(async () =>
        {
            var group = _mapper.Map<Klacks.Api.Domain.Models.Associations.Group>(request.Resource);
            await _groupRepository.Add(group);
            await _unitOfWork.CompleteAsync();
            return _mapper.Map<GroupResource>(group);
        }, 
        "creating group", 
        new { GroupId = request.Resource?.Id });
    }
}