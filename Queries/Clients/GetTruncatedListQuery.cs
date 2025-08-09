using Klacks.Api.Presentation.DTOs.Filter;
using MediatR;

namespace Klacks.Api.Queries.Clients;

public record GetTruncatedListQuery(FilterResource Filter) : IRequest<TruncatedClientResource>;
