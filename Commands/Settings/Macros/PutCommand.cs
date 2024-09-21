using Klacks.Api.Resources.Settings;
using MediatR;

namespace Klacks.Api.Commands.Settings.Macros;

public record PutCommand(MacroResource model) : IRequest<MacroResource>;
