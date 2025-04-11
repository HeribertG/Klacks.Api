using Klacks.Api.Commands.Groups;
using Klacks.Api.Datas;
using Klacks.Api.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Klacks.Api.Handlers.Groups;

/// <summary>
/// Handler für das DeleteGroupNodeCommand
/// </summary>
public class DeleteGroupNodeCommandHandler : IRequestHandler<DeleteGroupNodeCommand, bool>
{
    private readonly IGroupNestedSetRepository _repository;
    private readonly DataBaseContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DeleteGroupNodeCommandHandler(
        IGroupNestedSetRepository repository,
        DataBaseContext context,
        IHttpContextAccessor httpContextAccessor)
    {
        _repository = repository;
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<bool> Handle(DeleteGroupNodeCommand request, CancellationToken cancellationToken)
    {
        var currentUser = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name) ?? "System";

        var group = await _context.Group
            .FirstOrDefaultAsync(g => g.Id == request.Id && !g.IsDeleted, cancellationToken);

        if (group == null)
        {
            throw new KeyNotFoundException($"Gruppe mit ID {request.Id} nicht gefunden");
        }

        group.CurrentUserDeleted = currentUser;
        _context.Group.Update(group);
        await _context.SaveChangesAsync(cancellationToken);

        await _repository.DeleteNode(request.Id);

        return true;
    }
}
