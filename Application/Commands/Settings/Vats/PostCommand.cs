using MediatR;

namespace Klacks.Api.Commands.Settings.Vats;

public record PostCommand(Models.Settings.Vat model) : IRequest<Models.Settings.Vat>;
