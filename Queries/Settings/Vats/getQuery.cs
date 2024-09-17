using MediatR;

namespace Klacks_api.Queries.Settings.Vats;

public record GetQuery(Guid Id) : IRequest<Models.Settings.Vat>;
