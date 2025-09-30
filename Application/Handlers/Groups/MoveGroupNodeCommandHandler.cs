using AutoMapper;
using Klacks.Api.Application.Commands.Groups;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.Groups;

public class MoveGroupNodeCommandHandler : IRequestHandler<MoveGroupNodeCommand, GroupResource>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MoveGroupNodeCommandHandler> _logger;

    public MoveGroupNodeCommandHandler(
        IGroupRepository groupRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<MoveGroupNodeCommandHandler> logger)
    {
        _groupRepository = groupRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GroupResource> Handle(MoveGroupNodeCommand request, CancellationToken cancellationToken)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            _logger.LogInformation("Move node {NodeId} to new parent {NewParentId}", request.NodeId, request.NewParentId);
            
            await _groupRepository.MoveNode(request.NodeId, request.NewParentId);
            
            var movedGroup = await _groupRepository.Get(request.NodeId);
            if (movedGroup == null)
            {
                throw new KeyNotFoundException($"Group with ID {request.NodeId} not found after move");
            }
            
            var depth = await _groupRepository.GetNodeDepth(request.NodeId);
            var result = _mapper.Map<GroupResource>(movedGroup);
            result.Depth = depth;
            
            await _unitOfWork.CompleteAsync();
            await _unitOfWork.CommitTransactionAsync(transaction);
            
            _logger.LogInformation("Node {NodeId} successfully moved to parent {NewParentId}", request.NodeId, request.NewParentId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when moving the node {NodeId} to parent {NewParentId}", request.NodeId, request.NewParentId);
            await _unitOfWork.RollbackTransactionAsync(transaction);
            throw;
        }
    }
}