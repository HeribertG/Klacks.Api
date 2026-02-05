using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Groups;

public record GetGroupTreeQuery(Guid? RootId) : IRequest<GroupTreeResource>;
