using Klacks.Api.Presentation.Resources.Filter;
using MediatR;

namespace Klacks.Api.Queries.Shifts;

public record GetTruncatedListQuery(ShiftFilter Filter) : IRequest<TruncatedShiftResource>;
