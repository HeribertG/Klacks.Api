using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.GroupVisibilities;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<GroupVisibilityResource>, GroupVisibilityResource?>
{
    private readonly IGroupVisibilityRepository _groupVisibilityRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCommandHandler(
        IGroupVisibilityRepository groupVisibilityRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _groupVisibilityRepository = groupVisibilityRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<GroupVisibilityResource?> Handle(DeleteCommand<GroupVisibilityResource> request, CancellationToken cancellationToken)
    {
        var existingGroupVisibility = await _groupVisibilityRepository.Get(request.Id);
        if (existingGroupVisibility == null)
        {
            return null;
        }

        var groupVisibilityResource = _mapper.Map<GroupVisibilityResource>(existingGroupVisibility);
        await _groupVisibilityRepository.Delete(request.Id);
        await _unitOfWork.CompleteAsync();
        return groupVisibilityResource;
    }
}
