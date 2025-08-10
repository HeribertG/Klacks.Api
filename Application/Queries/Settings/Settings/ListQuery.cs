using MediatR;

namespace Klacks.Api.Application.Queries.Settings.Settings;

public record ListQuery : IRequest<IEnumerable<Klacks.Api.Domain.Models.Settings.Settings>>;
