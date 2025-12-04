using Klacks.Api.Presentation.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Groups;

public record GetGroupMembersQuery(Guid GroupId) : IRequest<List<GroupItemResource>>;