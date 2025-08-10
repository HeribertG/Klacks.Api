using MediatR;

namespace Klacks.Api.Application.Queries.Settings.Vats;

public record ListQuery : IRequest<IEnumerable<Klacks.Api.Domain.Models.Settings.Vat>>;
