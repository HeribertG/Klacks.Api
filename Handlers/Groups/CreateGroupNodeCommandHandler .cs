using Klacks.Api.Commands.Groups;
using Klacks.Api.Datas;
using Klacks.Api.Interfaces;
using Klacks.Api.Models.Associations;
using Klacks.Api.Queries.Groups;
using Klacks.Api.Resources.Associations;
using MediatR;
using System.Security.Claims;

namespace Klacks.Api.Handlers.Groups;

public class CreateGroupNodeCommandHandler : IRequestHandler<CreateGroupNodeCommand, GroupTreeNodeResource>
{
    private readonly IGroupRepository _repository;
    private readonly DataBaseContext _context;
    private readonly IMediator _mediator;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUnitOfWork _unitOfWork;

    public CreateGroupNodeCommandHandler(
        IGroupRepository repository,
        DataBaseContext context,
        IMediator mediator,
        IHttpContextAccessor httpContextAccessor,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _context = context;
        _mediator = mediator;
        _httpContextAccessor = httpContextAccessor;
        _unitOfWork = unitOfWork;
    }

    public async Task<GroupTreeNodeResource> Handle(CreateGroupNodeCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var transaction = await _unitOfWork.BeginTransactionAsync();

            try
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

                await _unitOfWork.CompleteAsync();

                if (!request.ParentId.HasValue)
                {
                    createdGroup.Root = createdGroup.Id;
                    _context.Group.Update(createdGroup);
                    await _unitOfWork.CompleteAsync();
                }

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
                    await _unitOfWork.CompleteAsync();
                }

                await _unitOfWork.CommitTransactionAsync(transaction);

                return await _mediator.Send(new GetGroupNodeDetailsQuery(createdGroup.Id), cancellationToken);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(transaction);
                throw;
            }
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Transactions are not supported"))
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

            await _unitOfWork.CompleteAsync();

            if (!request.ParentId.HasValue)
            {
                createdGroup.Root = createdGroup.Id;
                _context.Group.Update(createdGroup);
                await _unitOfWork.CompleteAsync();
            }

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
                await _unitOfWork.CompleteAsync();
            }

            return await _mediator.Send(new GetGroupNodeDetailsQuery(createdGroup.Id), cancellationToken);
        }
    }
}