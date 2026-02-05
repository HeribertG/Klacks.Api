using Klacks.Api.Application.Commands.PeriodClosures;
using Klacks.Api.Application.Handlers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Schedules;
using System.Security.Claims;

namespace Klacks.Api.Application.Handlers.PeriodClosures;

public class ClosePeriodCommandHandler : BaseHandler, IRequestHandler<ClosePeriodCommand, PeriodClosureResource>
{
    private readonly IPeriodClosureRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ClosePeriodCommandHandler(
        IPeriodClosureRepository repository,
        IUnitOfWork unitOfWork,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ClosePeriodCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<PeriodClosureResource> Handle(ClosePeriodCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var existing = await _repository.GetByDateRange(request.StartDate, request.EndDate);
            if (existing.Count > 0)
                throw new Domain.Exceptions.InvalidRequestException("An overlapping period closure already exists.");

            var userName = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown";

            var closure = new PeriodClosure
            {
                Id = Guid.NewGuid(),
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                ClosedBy = userName,
                ClosedAt = DateTime.UtcNow
            };

            await _repository.Add(closure);
            await _unitOfWork.CompleteAsync();

            return new PeriodClosureResource
            {
                Id = closure.Id,
                StartDate = closure.StartDate,
                EndDate = closure.EndDate,
                ClosedBy = closure.ClosedBy,
                ClosedAt = closure.ClosedAt
            };
        },
        "closing period",
        new { request.StartDate, request.EndDate });
    }
}
