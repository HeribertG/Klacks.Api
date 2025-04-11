using MediatR;

namespace Klacks.Api.Commands.Groups;

/// <summary>
/// Command zum Löschen einer Gruppe und all ihrer Untergruppen
/// </summary>
public record DeleteGroupNodeCommand(Guid Id) : IRequest<bool>;
