using MediatR;

namespace Klacks.Api.Application.Commands.Settings.Vats;

public record PutCommand(Models.Settings.Vat model) : IRequest<Models.Settings.Vat>;
