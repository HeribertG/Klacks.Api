using Klacks.Api.Commands.Groups;
using Klacks.Api.Interfaces;
using Klacks.Api.Queries.Groups;
using Klacks.Api.Resources.Associations;
using MediatR;

namespace Klacks.Api.Handlers.Groups;

/// <summary>
/// Handler für das MoveGroupNodeCommand
/// </summary>
public class MoveGroupNodeCommandHandler : IRequestHandler<MoveGroupNodeCommand, GroupTreeNodeResource>
{
    private readonly IGroupRepository _repository;
    private readonly IMediator _mediator;

    public MoveGroupNodeCommandHandler(IGroupRepository repository, IMediator mediator)
    {
        _repository = repository;
        _mediator = mediator;
    }

    public async Task<GroupTreeNodeResource> Handle(MoveGroupNodeCommand request, CancellationToken cancellationToken)
    {
        await _repository.MoveNode(request.NodeId, request.NewParentId);

        // Verwende den bestehenden Query-Handler, um die aktualisierten Details abzurufen
        return await _mediator.Send(new GetGroupNodeDetailsQuery(request.NodeId), cancellationToken);
    }
}
