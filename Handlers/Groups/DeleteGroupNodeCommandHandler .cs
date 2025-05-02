using Klacks.Api.Commands.Groups;
using Klacks.Api.Datas;
using Klacks.Api.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Klacks.Api.Handlers.Groups;

public class DeleteGroupNodeCommandHandler : IRequestHandler<DeleteGroupNodeCommand, bool>
{
    private readonly IGroupRepository _repository;
    private readonly DataBaseContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteGroupNodeCommandHandler> _logger;

    public DeleteGroupNodeCommandHandler(
        IGroupRepository repository,
        DataBaseContext context,
        IHttpContextAccessor httpContextAccessor,
        IUnitOfWork unitOfWork,
        ILogger<DeleteGroupNodeCommandHandler> logger)
    {
        _repository = repository;
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteGroupNodeCommand request, CancellationToken cancellationToken)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            var currentUser = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name) ?? "System";

            var group = await _context.Group
                .FirstOrDefaultAsync(g => g.Id == request.Id && !g.IsDeleted, cancellationToken);

            if (group == null)
            {
                throw new KeyNotFoundException($"Group with ID {request.Id} not found");
            }


            group.CurrentUserDeleted = currentUser;
            group.IsDeleted = true;
            group.DeletedTime = DateTime.UtcNow;
            _context.Group.Update(group);

   
            await _unitOfWork.CompleteAsync();

        
            await _repository.DeleteNode(request.Id);

            await _unitOfWork.CommitTransactionAsync(transaction);

            _logger.LogInformation($"Group with ID {request.Id} successfully deleted by user {currentUser}");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when deleting the group with ID {request.Id}");
            await _unitOfWork.RollbackTransactionAsync(transaction);
            throw;
        }
    }
}