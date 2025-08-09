using MediatR;

namespace Klacks.Api.Application.Commands.Settings.MacrosTypes;

public record PostCommand(Models.Settings.MacroType model) : IRequest<Models.Settings.MacroType>;
