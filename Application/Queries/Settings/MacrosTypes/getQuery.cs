using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Settings.MacrosTypes;

public record GetQuery(Guid Id) : IRequest<Klacks.Api.Domain.Models.Settings.MacroType>;
