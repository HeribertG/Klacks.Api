using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Settings.MacrosTypes;

public record ListQuery : IRequest<IEnumerable<Klacks.Api.Domain.Models.Settings.MacroType>>;
