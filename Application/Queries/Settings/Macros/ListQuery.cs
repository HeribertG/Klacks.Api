using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Settings.Macros;

public record ListQuery : IRequest<IEnumerable<MacroResource>>;
