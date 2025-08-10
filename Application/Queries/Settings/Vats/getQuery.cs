using MediatR;

namespace Klacks.Api.Application.Queries.Settings.Vats;

public record GetQuery(Guid Id) : IRequest<Klacks.Api.Domain.Models.Settings.Vat>;
