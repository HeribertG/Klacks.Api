using Klacks.Api.Presentation.Resources.Filter;
using MediatR;

namespace Klacks.Api.Queries.Clients;

public record GetTruncatedListQuery(FilterResource Filter) : IRequest<TruncatedClientResource>;
