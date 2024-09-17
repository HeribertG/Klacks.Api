using MediatR;

namespace Klacks_api.Commands.Settings.MacrosTypes;

public record DeleteCommand(Guid Id) : IRequest<Models.Settings.MacroType>;
