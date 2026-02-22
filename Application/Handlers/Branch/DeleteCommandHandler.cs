// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Settings.Branch;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;

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

        var deleted = await _branchRepository.Delete(request.id);
        if (deleted == null)
        {
            _logger.LogWarning("Branch not found: {BranchId}", request.id);
        }
        else
        {
            _logger.LogInformation("Branch entity marked for deletion: {BranchName}", deleted.Name);
        }

        await _unitOfWork.CompleteAsync();
        _logger.LogInformation("SaveChanges completed for branch deletion: {BranchId}", request.id);

        return Unit.Value;
    }
}
