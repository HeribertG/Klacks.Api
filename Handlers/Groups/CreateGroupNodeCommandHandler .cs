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
    /// Handler für das CreateGroupNodeCommand
    /// </summary>
    public class CreateGroupNodeCommandHandler : IRequestHandler<CreateGroupNodeCommand, GroupTreeNodeResource>
    {
        private readonly IGroupRepository _repository;
        private readonly DataBaseContext _context;
        private readonly IMediator _mediator;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CreateGroupNodeCommandHandler(
            IGroupRepository repository,
            DataBaseContext context,
            IMediator mediator,
            IHttpContextAccessor httpContextAccessor)
        {
            _repository = repository;
            _context = context;
            _mediator = mediator;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<GroupTreeNodeResource> Handle(CreateGroupNodeCommand request, CancellationToken cancellationToken)
        {
            var currentUser = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name) ?? "System";

            var newGroup = new Group
            {
                Name = request.Group.Name,
                Description = request.Group.Description,
                ValidFrom = request.Group.ValidFrom,
                ValidUntil = request.Group.ValidUntil,
                CurrentUserCreated = currentUser,
                GroupItems = new List<GroupItem>()
            };

            Group createdGroup;

            if (request.ParentId.HasValue)
            {
                createdGroup = await _repository.AddChildNode(request.ParentId.Value, newGroup);
            }
            else
            {
                createdGroup = await _repository.AddRootNode(newGroup);
            }

            // Aktualisiere die Gruppenmitglieder, falls vorhanden
            if (request.Group.ClientIds != null && request.Group.ClientIds.Any())
            {
                foreach (var clientId in request.Group.ClientIds)
                {
                    var groupItem = new GroupItem
                    {
                        GroupId = createdGroup.Id,
                        ClientId = clientId
                    };
                    _context.GroupItem.Add(groupItem);
                }
                await _context.SaveChangesAsync(cancellationToken);
            }

            // Verwende den bestehenden Query-Handler, um die Details abzurufen
            return await _mediator.Send(new GetGroupNodeDetailsQuery(createdGroup.Id), cancellationToken);
        }
    }