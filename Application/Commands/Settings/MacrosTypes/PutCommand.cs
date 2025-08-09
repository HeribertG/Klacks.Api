using MediatR;

namespace Klacks.Api.Application.Commands.Settings.MacrosTypes;

public record PutCommand(Models.Settings.MacroType model) : IRequest<Models.Settings.MacroType>;
