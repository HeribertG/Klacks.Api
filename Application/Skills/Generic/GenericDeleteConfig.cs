// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Configuration record for generic delete skill execution via reflection.
/// @param RepositoryInterface - DI interface name to resolve (e.g. "IBranchRepository")
/// @param GetMethod - Repository method to retrieve an entity by ID (e.g. "Get")
/// @param DeleteMethod - Repository method to delete an entity by ID (e.g. "Delete")
/// @param IdParameter - Skill parameter name that holds the entity ID
/// @param EntityLabel - Human-readable label for the entity type used in result messages
/// @param NameField - Property name on the entity used to read its display name
/// @param RequiresUnitOfWork - Whether to call IUnitOfWork.CompleteAsync() after deletion
/// </summary>
namespace Klacks.Api.Application.Skills.Generic;

public class GenericDeleteConfig
{
    public string RepositoryInterface { get; set; } = string.Empty;
    public string GetMethod { get; set; } = "Get";
    public string DeleteMethod { get; set; } = "Delete";
    public string IdParameter { get; set; } = "id";
    public string EntityLabel { get; set; } = "Entity";
    public string NameField { get; set; } = "Name";
    public bool RequiresUnitOfWork { get; set; } = true;
}
