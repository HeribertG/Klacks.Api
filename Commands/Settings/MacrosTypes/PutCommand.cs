using MediatR;

namespace Klacks.Api.Commands.Settings.MacrosTypes;

public record PutCommand(Models.Settings.MacroType model) : IRequest<Models.Settings.MacroType>;
