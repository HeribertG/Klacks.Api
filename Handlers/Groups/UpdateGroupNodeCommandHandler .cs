using Klacks.Api.Commands.Groups;
using Klacks.Api.Datas;
using Klacks.Api.Interfaces;
using Klacks.Api.Models.Associations;
using Klacks.Api.Queries.Groups;
using Klacks.Api.Resources.Associations;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Klacks.Api.Handlers.Groups;

/// <summary>
/// Handler für das UpdateGroupNodeCommand
/// </summary>
public class UpdateGroupNodeCommandHandler : IRequestHandler<UpdateGroupNodeCommand, GroupTreeNodeResource>
{
    private readonly IGroupNestedSetRepository _repository;
    private readonly DataBaseContext _context;
    private readonly IMediator _mediator;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UpdateGroupNodeCommandHandler(
        IGroupNestedSetRepository repository,
        DataBaseContext context,
        IMediator mediator,
        IHttpContextAccessor httpContextAccessor)
    {
        _repository = repository;
        _context = context;
        _mediator = mediator;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<GroupTreeNodeResource> Handle(UpdateGroupNodeCommand request, CancellationToken cancellationToken)
    {
        var currentUser = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name) ?? "System";

        var group = await _context.Group
            .Include(g => g.GroupItems)
            .FirstOrDefaultAsync(g => g.Id == request.Id && !g.IsDeleted, cancellationToken);

        if (group == null)
        {
            throw new KeyNotFoundException($"Gruppe mit ID {request.Id} nicht gefunden");
        }

        // Grundlegende Daten aktualisieren
        group.Name = request.Group.Name;
        group.Description = request.Group.Description;
        group.ValidFrom = request.Group.ValidFrom;
        group.ValidUntil = request.Group.ValidUntil;
        group.CurrentUserUpdated = currentUser;

        // Wenn ein neuer Elternteil angegeben wird und sich geändert hat
        if (request.Group.ParentId.HasValue && group.Parent != request.Group.ParentId)
        {
            await _repository.MoveNode(request.Id, request.Group.ParentId.Value);
        }

        // Gruppenmitglieder aktualisieren
        if (request.Group.ClientIds != null)
        {
            var existingClientIds = group.GroupItems.Select(gi => gi.ClientId).ToList();
            var newClientIds = request.Group.ClientIds.ToList();

            // Zu entfernende Mitglieder
            var clientsToRemove = existingClientIds.Except(newClientIds).ToList();
            foreach (var clientId in clientsToRemove)
            {
                var groupItem = group.GroupItems.FirstOrDefault(gi => gi.ClientId == clientId);
                if (groupItem != null)
                {
                    _context.GroupItem.Remove(groupItem);
                }
            }

            // Neue Mitglieder hinzufügen
            var clientsToAdd = newClientIds.Except(existingClientIds).ToList();
            foreach (var clientId in clientsToAdd)
            {
                var groupItem = new GroupItem
                {
                    GroupId = request.Id,
                    ClientId = clientId
                };
                _context.GroupItem.Add(groupItem);
            }
        }

        await _repository.UpdateNode(group);
        await _context.SaveChangesAsync(cancellationToken);

        // Verwende den bestehenden Query-Handler, um die aktualisierten Details abzurufen
        return await _mediator.Send(new GetGroupNodeDetailsQuery(request.Id), cancellationToken);
    }
}
