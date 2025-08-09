using Klacks.Api.Models.Staffs;
using MediatR;

namespace Klacks.Api.Application.Queries.AssignedGroups;

public record AssignedGroupListQuery(Guid? Id = null) : IRequest<IEnumerable<AssignedGroup>>;

