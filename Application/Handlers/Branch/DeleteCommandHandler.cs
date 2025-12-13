using Klacks.Api.Application.Commands.Settings.Branch;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Branch;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand>
{
    private readonly IBranchRepository _branchRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCommandHandler(
        IBranchRepository branchRepository,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _branchRepository = branchRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeleteCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting branch with ID: {BranchId}", request.id);

        await _branchRepository.Delete(request.id);
        await _unitOfWork.CompleteAsync();

        _logger.LogInformation("Branch deleted successfully: {BranchId}", request.id);
        return Unit.Value;
    }
}
