using Klacks_api.Resources.Settings;
using MediatR;

namespace Klacks_api.Commands.Settings.Macros;

public record PutCommand(MacroResource model) : IRequest<MacroResource>;
