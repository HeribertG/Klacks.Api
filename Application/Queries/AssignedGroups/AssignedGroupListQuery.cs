using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.AssignedGroups;

public record AssignedGroupListQuery(Guid? Id = null) : IRequest<IEnumerable<GroupResource>>;

