using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.GroupItems;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<GroupItemResource>, GroupItemResource?>
{
    private readonly IGroupItemRepository _groupItemRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public PostCommandHandler(
        IGroupItemRepository groupItemRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _groupItemRepository = groupItemRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<GroupItemResource?> Handle(PostCommand<GroupItemResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var groupItem = _mapper.Map<GroupItem>(request.Resource);
            await _groupItemRepository.Add(groupItem);
            await _unitOfWork.CompleteAsync();
            return _mapper.Map<GroupItemResource>(groupItem);
        },
        "creating group item",
        new { });
    }
}
