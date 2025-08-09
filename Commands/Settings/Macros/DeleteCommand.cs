using Klacks.Api.Presentation.Resources.Settings;
using MediatR;

namespace Klacks.Api.Commands.Settings.Macros;

public record DeleteCommand(Guid Id) : IRequest<MacroResource>;
