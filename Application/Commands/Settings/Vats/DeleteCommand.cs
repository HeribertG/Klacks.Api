using MediatR;

namespace Klacks.Api.Application.Commands.Settings.Vats;

public record DeleteCommand(Guid Id) : IRequest<Models.Settings.Vat>;
