using MediatR;

namespace Klacks.Api.Application.Commands.Settings.MacrosTypes;

public record DeleteCommand(Guid Id) : IRequest<Klacks.Api.Domain.Models.Settings.MacroType>;
