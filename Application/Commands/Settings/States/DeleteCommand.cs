using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Settings.States;

public record DeleteCommand(Guid Id) : IRequest<Klacks.Api.Domain.Models.Settings.State>;
