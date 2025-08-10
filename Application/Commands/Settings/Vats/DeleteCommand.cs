using MediatR;

namespace Klacks.Api.Application.Commands.Settings.Vats;

public record DeleteCommand(Guid Id) : IRequest<Klacks.Api.Domain.Models.Settings.Vat>;
