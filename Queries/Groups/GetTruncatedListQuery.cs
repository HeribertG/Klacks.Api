using Klacks.Api.Resources.Filter;
using MediatR;

namespace Klacks.Api.Queries.Groups;

public record GetTruncatedListQuery(GroupFilter Filter) : IRequest<TruncatedGroupResource>;
