using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.GroupItems;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<GroupItemResource>, GroupItemResource?>
{
    private readonly IGroupItemRepository _groupItemRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCommandHandler(
        IGroupItemRepository groupItemRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _groupItemRepository = groupItemRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<GroupItemResource?> Handle(DeleteCommand<GroupItemResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var groupItem = await _groupItemRepository.Delete(request.Id);
            await _unitOfWork.CompleteAsync();
            return _mapper.Map<GroupItemResource>(groupItem);
        },
        "deleting group item",
        new { Id = request.Id });
    }
}
