using MediatR;

namespace Klacks_api.Commands.Settings.Vats;

public record DeleteCommand(Guid Id) : IRequest<Models.Settings.Vat>;
