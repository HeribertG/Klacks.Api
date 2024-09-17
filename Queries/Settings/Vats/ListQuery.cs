using MediatR;

namespace Klacks_api.Queries.Settings.Vats;

public record ListQuery : IRequest<IEnumerable<Models.Settings.Vat>>;
