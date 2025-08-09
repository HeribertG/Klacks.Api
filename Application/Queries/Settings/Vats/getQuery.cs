using MediatR;

namespace Klacks.Api.Application.Queries.Settings.Vats;

public record GetQuery(Guid Id) : IRequest<Models.Settings.Vat>;
