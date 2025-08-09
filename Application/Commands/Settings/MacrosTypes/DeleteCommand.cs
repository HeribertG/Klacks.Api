using MediatR;

namespace Klacks.Api.Application.Commands.Settings.MacrosTypes;

public record DeleteCommand(Guid Id) : IRequest<Models.Settings.MacroType>;
