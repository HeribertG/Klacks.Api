using MediatR;

namespace Klacks.Api.Commands.Settings.MacrosTypes;

public record DeleteCommand(Guid Id) : IRequest<Models.Settings.MacroType>;
