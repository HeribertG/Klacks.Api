using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Groups;

public record GetGroupVisibilityListQuery(Guid Id) : IRequest<IEnumerable<GroupResource>>;

