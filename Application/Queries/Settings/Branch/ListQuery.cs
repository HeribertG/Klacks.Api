using MediatR;

namespace Klacks.Api.Application.Queries.Settings.Branch;

public record ListQuery() : IRequest<IEnumerable<Klacks.Api.Domain.Models.Settings.Branch>>;
