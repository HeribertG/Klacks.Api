using MediatR;

namespace Klacks.Api.Queries.Groups;

public record GetRootsQuery() : IRequest<IEnumerable<GroupResource>>;

