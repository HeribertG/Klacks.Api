using MediatR;

namespace Klacks.Api.Commands.Settings.Vats;

public record DeleteCommand(Guid Id) : IRequest<Models.Settings.Vat>;
