using Klacks.Api.Application.Commands.PeriodClosures;
using Klacks.Api.Application.Handlers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.PeriodClosures;

public class ReopenPeriodCommandHandler : BaseHandler, IRequestHandler<ReopenPeriodCommand, bool>
{
    private readonly IPeriodClosureRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ReopenPeriodCommandHandler(
        IPeriodClosureRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<ReopenPeriodCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(ReopenPeriodCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var closure = await _repository.Get(request.Id);
            if (closure == null)
                throw new KeyNotFoundException($"PeriodClosure with ID {request.Id} not found.");

            await _repository.Delete(request.Id);
            await _unitOfWork.CompleteAsync();

            return true;
        },
        "reopening period",
        new { request.Id });
    }
}
