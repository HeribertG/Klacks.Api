using MediatR;

namespace Klacks.Api.Queries.Settings.Vats;

public record ListQuery : IRequest<IEnumerable<Models.Settings.Vat>>;
