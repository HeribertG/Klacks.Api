using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Queries.Groups;

public record GetGroupMembersQuery(Guid GroupId) : IRequest<List<GroupItemResource>>;