using MediatR;

namespace Klacks.Api.Application.Commands.Settings.Vats;

public record PutCommand(Klacks.Api.Domain.Models.Settings.Vat model) : IRequest<Klacks.Api.Domain.Models.Settings.Vat>;
