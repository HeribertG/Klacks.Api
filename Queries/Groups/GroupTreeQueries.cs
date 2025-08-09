using Klacks.Api.Presentation.Resources.Associations;
using MediatR;

namespace Klacks.Api.Queries.Groups;

public record GetGroupTreeQuery(Guid? RootId) : IRequest<GroupTreeResource>;
