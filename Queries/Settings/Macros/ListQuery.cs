using Klacks_api.Resources.Settings;
using MediatR;

namespace Klacks_api.Queries.Settings.Macros;

public record ListQuery : IRequest<IEnumerable<MacroResource>>;
