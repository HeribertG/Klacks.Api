// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Microsoft.Extensions.Logging;

namespace Klacks.Api.Infrastructure.MCP;

public class MCPService : IMCPService, IDisposable
{
    private readonly ILogger<MCPService> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private MCPClient? _client;
    private readonly object _lock = new();
    private bool _isConnected;

    public MCPService(ILogger<MCPService> logger, ILoggerFactory loggerFactory)
    {
        this._logger = logger;
        _loggerFactory = loggerFactory;
    }

    public async Task<bool> ConnectAsync()
    {
        lock (_lock)
        {
            if (_isConnected)
            {
                return true;
            }

            try
            {
                _client = new MCPClient(_loggerFactory.CreateLogger<MCPClient>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create MCP Client");
                return false;
            }
        }

        var connected = await _client.ConnectAsync();
        
        lock (_lock)
        {
            _isConnected = connected;
        }

        return connected;
    }

    public async Task<List<MCPTool>> GetAvailableToolsAsync()
    {
        await EnsureConnectedAsync();
        return await _client!.GetAvailableToolsAsync();
    }

    public async Task<string> ExecuteToolAsync(string toolName, object arguments)
    {
        await EnsureConnectedAsync();
        return await _client!.CallToolAsync(toolName, arguments);
    }

    public async Task<List<MCPResource>> GetAvailableResourcesAsync()
    {
        await EnsureConnectedAsync();
        return await _client!.GetAvailableResourcesAsync();
    }

    public async Task<string> ReadResourceAsync(string uri)
    {
        await EnsureConnectedAsync();
        return await _client!.ReadResourceAsync(uri);
    }

    public Task DisconnectAsync()
    {
        lock (_lock)
        {
            if (!_isConnected)
            {
                return Task.CompletedTask;
            }

            _client?.Dispose();
            _client = null;
            _isConnected = false;
        }

        _logger.LogInformation("MCP Service disconnected");
        return Task.CompletedTask;
    }

    private async Task EnsureConnectedAsync()
    {
        lock (_lock)
        {
            if (_isConnected && _client != null)
            {
                return;
            }
        }

        var connected = await ConnectAsync();
        if (!connected)
        {
            throw new InvalidOperationException("Failed to connect to MCP Server");
        }
    }

    public void Dispose()
    {
        DisconnectAsync().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }
}