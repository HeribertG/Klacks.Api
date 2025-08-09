using MediatR;

namespace Klacks.Api.Commands.Settings.MacrosTypes;

public record PostCommand(Models.Settings.MacroType model) : IRequest<Models.Settings.MacroType>;
