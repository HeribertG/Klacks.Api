using AutoMapper;
using Klacks.Api.Application.Commands.Groups;
using Klacks.Api.Datas;
using Klacks.Api.Interfaces;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Handlers.Groups;

public class MoveGroupNodeCommandHandler : IRequestHandler<MoveGroupNodeCommand, GroupResource>
{
    private readonly IGroupRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MoveGroupNodeCommandHandler> _logger;
    private readonly IMapper _mapper;
    private readonly DataBaseContext _context;

    public MoveGroupNodeCommandHandler(
        IGroupRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<MoveGroupNodeCommandHandler> logger,
        IMapper mapper,
        DataBaseContext context)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _mapper = mapper;
        _context = context;
    }

    public async Task<GroupResource> Handle(MoveGroupNodeCommand request, CancellationToken cancellationToken)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            _logger.LogInformation($"Move node {request.NodeId} to new parent {request.NewParentId}");

            await _repository.MoveNode(request.NodeId, request.NewParentId);
            await _unitOfWork.CompleteAsync();
            await _unitOfWork.CommitTransactionAsync(transaction);

            _logger.LogInformation($"Node {request.NodeId} successfully moved to parent {request.NewParentId}");

            // Lade die Gruppe mit allen benötigten Beziehungen
            var node = await _context.Group
                .Include(g => g.GroupItems)
                .ThenInclude(gi => gi.Client)
                .FirstOrDefaultAsync(g => g.Id == request.NodeId && !g.IsDeleted, cancellationToken);

            if (node == null)
            {
                throw new KeyNotFoundException($"Group with ID {request.NodeId} not found after move");
            }

            // Berechne die Tiefe für den Knoten
            var depth = await _repository.GetNodeDepth(request.NodeId);

            // Transformiere die Gruppe in eine GroupTreeNodeResource mit AutoMapper
            var result = _mapper.Map<GroupResource>(node);

            // Nur die Tiefe muss manuell gesetzt werden, da sie nicht automatisch gemappt wird
            result.Depth = depth;

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when moving the node {request.NodeId} to parent {request.NewParentId}");
            await _unitOfWork.RollbackTransactionAsync(transaction);
            throw;
        }
    }
}