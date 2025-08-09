using MediatR;

namespace Klacks.Api.Queries.Settings.Vats;

public record GetQuery(Guid Id) : IRequest<Models.Settings.Vat>;
