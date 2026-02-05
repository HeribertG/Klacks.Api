using Klacks.Api.Application.Commands.DayApprovals;
using Klacks.Api.Application.Handlers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.DayApprovals;

public class RevokeDayApprovalCommandHandler : BaseHandler, IRequestHandler<RevokeDayApprovalCommand, bool>
{
    private readonly IDayApprovalRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public RevokeDayApprovalCommandHandler(
        IDayApprovalRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<RevokeDayApprovalCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(RevokeDayApprovalCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var approval = await _repository.Get(request.Id);
            if (approval == null)
                throw new KeyNotFoundException($"DayApproval with ID {request.Id} not found.");

            await _repository.Delete(request.Id);
            await _unitOfWork.CompleteAsync();

            return true;
        },
        "revoking day approval",
        new { request.Id });
    }
}
