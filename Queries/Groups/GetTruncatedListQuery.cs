using Klacks.Api.Presentation.Resources.Filter;
using MediatR;

namespace Klacks.Api.Queries.Groups;

public record GetTruncatedListQuery(GroupFilter Filter) : IRequest<TruncatedGroupResource>;
