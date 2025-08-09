using MediatR;

namespace Klacks.Api.Application.Queries.Settings.Vats;

public record ListQuery : IRequest<IEnumerable<Models.Settings.Vat>>;
