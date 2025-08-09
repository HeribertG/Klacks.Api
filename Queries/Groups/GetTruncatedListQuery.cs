using Klacks.Api.Presentation.DTOs.Filter;
using MediatR;

namespace Klacks.Api.Queries.Groups;

public record GetTruncatedListQuery(GroupFilter Filter) : IRequest<TruncatedGroupResource>;
