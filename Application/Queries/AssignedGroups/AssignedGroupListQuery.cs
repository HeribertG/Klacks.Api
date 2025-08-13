using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Queries.AssignedGroups;

public record AssignedGroupListQuery(Guid? Id = null) : IRequest<IEnumerable<GroupResource>>;

