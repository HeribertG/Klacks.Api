using Klacks.Api.Commands.Groups;
using Klacks.Api.Interfaces;
using Klacks.Api.Queries.Groups;
using Klacks.Api.Resources.Associations;
using MediatR;

namespace Klacks.Api.Handlers.Groups;

public class MoveGroupNodeCommandHandler : IRequestHandler<MoveGroupNodeCommand, GroupTreeNodeResource>
{
    private readonly IGroupRepository _repository;
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MoveGroupNodeCommandHandler> _logger;

    public MoveGroupNodeCommandHandler(
        IGroupRepository repository,
        IMediator mediator,
        IUnitOfWork unitOfWork,
        ILogger<MoveGroupNodeCommandHandler> logger)
    {
        _repository = repository;
        _mediator = mediator;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GroupTreeNodeResource> Handle(MoveGroupNodeCommand request, CancellationToken cancellationToken)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            _logger.LogInformation($"Move node {request.NodeId} to new parent {request.NewParentId}");


            await _repository.MoveNode(request.NodeId, request.NewParentId);

            await _unitOfWork.CompleteAsync();

            await _unitOfWork.CommitTransactionAsync(transaction);

            _logger.LogInformation($"Node {request.NodeId} successfully moved to parent {request.NewParentId}");


            return await _mediator.Send(new GetGroupNodeDetailsQuery(request.NodeId), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when moving the node {request.NodeId} to parent {request.NewParentId}");
            await _unitOfWork.RollbackTransactionAsync(transaction);
            throw;
        }
    }
}