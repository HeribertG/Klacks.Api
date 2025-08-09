using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Queries.Groups;

public record GetGroupVisibilityListQuery(Guid Id) : IRequest<IEnumerable<GroupResource>>;

