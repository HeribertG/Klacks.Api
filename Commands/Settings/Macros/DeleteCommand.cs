using Klacks_api.Resources.Settings;
using MediatR;

namespace Klacks_api.Commands.Settings.Macros;

public record DeleteCommand(Guid Id) : IRequest<MacroResource>;
