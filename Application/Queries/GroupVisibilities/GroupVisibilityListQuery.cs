using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Queries.GroupVisibilities;

public record GroupVisibilityListQuery(string Id) : IRequest<IEnumerable<GroupVisibilityResource>>;
