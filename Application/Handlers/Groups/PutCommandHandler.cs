using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Groups;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<GroupResource>, GroupResource?>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    
    public PutCommandHandler(
        IGroupRepository groupRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _groupRepository = groupRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        }

    public async Task<GroupResource?> Handle(PutCommand<GroupResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var group = _mapper.Map<Klacks.Api.Domain.Models.Associations.Group>(request.Resource);
            var updatedGroup = await _groupRepository.Put(group);
            var result = _mapper.Map<GroupResource>(updatedGroup);

            await _unitOfWork.CompleteAsync();

            return result;
        }, 
        "updating", 
        new { });
    }
}
