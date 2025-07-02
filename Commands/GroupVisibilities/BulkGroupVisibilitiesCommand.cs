using Klacks.Api.Resources.Associations;
using MediatR;

namespace Klacks.Api.Commands.GroupVisibilities;

public record BulkGroupVisibilitiesCommand(List<GroupVisibilityResource> List) : IRequest;