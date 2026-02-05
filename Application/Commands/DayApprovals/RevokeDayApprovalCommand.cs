using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.DayApprovals;

public record RevokeDayApprovalCommand(Guid Id) : IRequest<bool>;
