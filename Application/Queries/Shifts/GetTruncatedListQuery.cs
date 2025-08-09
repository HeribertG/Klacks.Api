using Klacks.Api.Presentation.DTOs.Filter;
using MediatR;

namespace Klacks.Api.Application.Queries.Shifts;

public record GetTruncatedListQuery(ShiftFilter Filter) : IRequest<TruncatedShiftResource>;
