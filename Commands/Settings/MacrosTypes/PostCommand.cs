using MediatR;

namespace Klacks_api.Commands.Settings.MacrosTypes;

public record PostCommand(Models.Settings.MacroType model) : IRequest<Models.Settings.MacroType>;
