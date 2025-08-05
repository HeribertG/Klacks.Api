using Klacks.Api.Resources.Associations;
using MediatR;

namespace Klacks.Api.Queries.Groups;

public record GetGroupMembersQuery(Guid GroupId) : IRequest<List<GroupItemResource>>;