using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Queries.Groups;

public record GetGroupTreeQuery(Guid? RootId) : IRequest<GroupTreeResource>;
