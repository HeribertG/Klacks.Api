using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Schedules;

namespace Klacks.Api.Application.Commands.DayApprovals;

public record ApproveDayCommand(DateOnly ApprovalDate, Guid GroupId) : IRequest<DayApprovalResource>;
