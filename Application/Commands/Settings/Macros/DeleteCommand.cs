using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Commands.Settings.Macros;

public record DeleteCommand(Guid Id) : IRequest<MacroResource>;
