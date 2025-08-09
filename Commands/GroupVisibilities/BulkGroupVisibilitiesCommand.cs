using Klacks.Api.Presentation.Resources.Associations;
using MediatR;

namespace Klacks.Api.Commands.GroupVisibilities;

public record BulkGroupVisibilitiesCommand(List<GroupVisibilityResource> List) : IRequest;