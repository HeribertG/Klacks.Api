using MediatR;

namespace Klacks_api.Commands.Settings.Vats;

public record PostCommand(Models.Settings.Vat model) : IRequest<Models.Settings.Vat>;
