using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Queries.Shifts;

public record GetResetDateRangeQuery(Guid OriginalId) : IRequest<ResetDateRangeResponse>;
