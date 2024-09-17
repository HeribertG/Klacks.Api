using Klacks_api.Resources.Filter;
using MediatR;

namespace Klacks_api.Queries.Groups;


public record GetTruncatedListQuery(GroupFilter Filter) : IRequest<TruncatedGroupResource>;
