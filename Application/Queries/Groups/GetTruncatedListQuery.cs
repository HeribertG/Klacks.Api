using Klacks.Api.Presentation.DTOs.Filter;
using MediatR;

namespace Klacks.Api.Application.Queries.Groups;

public record GetTruncatedListQuery(GroupFilter Filter) : IRequest<TruncatedGroupResource>;
