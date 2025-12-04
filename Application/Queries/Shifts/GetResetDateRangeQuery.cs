using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Shifts;

public record GetResetDateRangeQuery(Guid OriginalId) : IRequest<ResetDateRangeResponse>;
