using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Settings.MacrosTypes;

public record PostCommand(Klacks.Api.Domain.Models.Settings.MacroType model) : IRequest<Klacks.Api.Domain.Models.Settings.MacroType>;
