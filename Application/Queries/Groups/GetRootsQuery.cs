using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Queries.Groups;

public record GetRootsQuery() : IRequest<IEnumerable<GroupResource>>;

