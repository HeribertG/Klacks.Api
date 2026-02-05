using Klacks.Api.Application.Commands.DayApprovals;
using Klacks.Api.Application.Handlers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Schedules;
using System.Security.Claims;

namespace Klacks.Api.Application.Handlers.DayApprovals;

public class ApproveDayCommandHandler : BaseHandler, IRequestHandler<ApproveDayCommand, DayApprovalResource>
{
    private readonly IDayApprovalRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApproveDayCommandHandler(
        IDayApprovalRepository repository,
        IUnitOfWork unitOfWork,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ApproveDayCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<DayApprovalResource> Handle(ApproveDayCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            if (await _repository.ExistsForDateAndGroup(request.ApprovalDate, request.GroupId))
                throw new Domain.Exceptions.InvalidRequestException("Day is already approved for this group.");

            var userName = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown";

            var dayApproval = new DayApproval
            {
                Id = Guid.NewGuid(),
                ApprovalDate = request.ApprovalDate,
                GroupId = request.GroupId,
                ApprovedBy = userName,
                ApprovedAt = DateTime.UtcNow
            };

            await _repository.Add(dayApproval);
            await _unitOfWork.CompleteAsync();

            return new DayApprovalResource
            {
                Id = dayApproval.Id,
                ApprovalDate = dayApproval.ApprovalDate,
                GroupId = dayApproval.GroupId,
                ApprovedBy = dayApproval.ApprovedBy,
                ApprovedAt = dayApproval.ApprovedAt
            };
        },
        "approving day",
        new { request.ApprovalDate, request.GroupId });
    }
}
