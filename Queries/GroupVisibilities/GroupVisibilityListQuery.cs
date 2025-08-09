using Klacks.Api.Presentation.Resources.Associations;
using MediatR;

namespace Klacks.Api.Queries.GroupVisibilities;

public record GroupVisibilityListQuery(string Id) : IRequest<IEnumerable<GroupVisibilityResource>>;
