// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Bridges the Contracts IPluginUnitOfWork to the Core IUnitOfWork.
/// </summary>

using Klacks.Api.Domain.Interfaces;
using Klacks.Plugin.Contracts;

namespace Klacks.Api.Infrastructure.Plugins;

public class PluginUnitOfWorkBridge : IPluginUnitOfWork
{
    private readonly IUnitOfWork _unitOfWork;

    public PluginUnitOfWorkBridge(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task CompleteAsync() => _unitOfWork.CompleteAsync();
}
