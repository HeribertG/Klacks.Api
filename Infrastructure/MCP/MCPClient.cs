// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Infrastructure.MCP;

public class MCPClient : IDisposable
{
    private readonly ILogger<MCPClient> _logger;
    private Process? _mcpServerProcess;
    private StreamWriter? _stdin;
    private StreamReader? _stdout;
    private readonly string _serverPath;
    private bool _isInitialized;
    private int _requestIdCounter;

    public MCPClient(ILogger<MCPClient> logger, string serverPath = "")
    {
        this._logger = logger;
        _serverPath = string.IsNullOrEmpty(serverPath) 
            ? Path.Combine(Directory.GetCurrentDirectory(), "..", "Klacks.MCP.Server")
            : serverPath;
    }

    public async Task<bool> ConnectAsync()
    {
        try
        {
            _logger.LogInformation("Starting MCP Server at: {ServerPath}", _serverPath);
            
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project \"{_serverPath}\"",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            _mcpServerProcess = Process.Start(startInfo);
            if (_mcpServerProcess == null)
            {
                _logger.LogError("Failed to start MCP Server process");
                return false;
            }

            _stdin = _mcpServerProcess.StandardInput;
            _stdout = _mcpServerProcess.StandardOutput;

            // Initialize the MCP connection
            var initResponse = await SendRequestAsync("initialize", new
            {
                protocolVersion = "2024-11-05",
                capabilities = new
                {
                    tools = new { }
                },
                clientInfo = new
                {
                    name = "klacks-api-client",
                    version = "1.0.0"
                }
            });

            _isInitialized = initResponse != null && initResponse.Error == null;
            
            if (_isInitialized)
            {
                _logger.LogInformation("MCP Client connected successfully");
            }
            else
            {
                _logger.LogError("Failed to initialize MCP connection: {Error}", 
                    initResponse?.Error?.Message ?? "Unknown error");
            }

            return _isInitialized;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting to MCP Server");
            return false;
        }
    }

    public async Task<List<MCPTool>> GetAvailableToolsAsync()
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("MCP Client is not connected");
        }

        try
        {
            var response = await SendRequestAsync("tools/list", null);
            if (response?.Result != null)
            {
                var resultJson = JsonSerializer.Serialize(response.Result);
                var toolsResult = JsonSerializer.Deserialize<ToolsListResult>(resultJson);
                return toolsResult?.Tools ?? new List<MCPTool>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available tools");
        }

        return new List<MCPTool>();
    }

    public async Task<string> CallToolAsync(string toolName, object arguments)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("MCP Client is not connected");
        }

        try
        {
            var response = await SendRequestAsync("tools/call", new
            {
                name = toolName,
                arguments = arguments
            });

            if (response?.Error != null)
            {
                _logger.LogError("Tool call error: {Error}", response.Error.Message);
                return $"❌ Error: {response.Error.Message}";
            }

            if (response?.Result != null)
            {
                var resultJson = JsonSerializer.Serialize(response.Result);
                var toolResult = JsonSerializer.Deserialize<ToolCallResult>(resultJson);
                
                if (toolResult?.Content?.Any() == true)
                {
                    var textContent = toolResult.Content
                        .Where(c => c.Type == "text")
                        .Select(c => c.Text)
                        .FirstOrDefault();
                    
                    return textContent ?? "Tool executed successfully";
                }
            }

            return "Tool executed successfully";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling tool: {ToolName}", toolName);
            return $"❌ Internal error calling tool {toolName}";
        }
    }

    public async Task<List<MCPResource>> GetAvailableResourcesAsync()
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("MCP Client is not connected");
        }

        try
        {
            var response = await SendRequestAsync("resources/list", null);
            if (response?.Result != null)
            {
                var resultJson = JsonSerializer.Serialize(response.Result);
                var resourcesResult = JsonSerializer.Deserialize<ResourcesListResult>(resultJson);
                return resourcesResult?.Resources ?? new List<MCPResource>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available resources");
        }

        return new List<MCPResource>();
    }

    public async Task<string> ReadResourceAsync(string uri)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("MCP Client is not connected");
        }

        try
        {
            var response = await SendRequestAsync("resources/read", new
            {
                uri = uri
            });

            if (response?.Error != null)
            {
                _logger.LogError("Resource read error: {Error}", response.Error.Message);
                return $"❌ Error reading resource: {response.Error.Message}";
            }

            if (response?.Result != null)
            {
                var resultJson = JsonSerializer.Serialize(response.Result);
                var resourceResult = JsonSerializer.Deserialize<ResourceReadResult>(resultJson);
                
                return resourceResult?.Contents?.FirstOrDefault()?.Text ?? "Resource read successfully";
            }

            return "Resource read successfully";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading resource: {Uri}", uri);
            return $"❌ Internal error reading resource {uri}";
        }
    }

    private async Task<MCPResponse?> SendRequestAsync(string method, object? parameters)
    {
        if (_stdin == null || _stdout == null)
        {
            throw new InvalidOperationException("MCP connection is not established");
        }

        var requestId = ++_requestIdCounter;
        var request = new MCPRequest
        {
            Id = requestId,
            Method = method,
            Params = parameters != null ? JsonSerializer.SerializeToElement(parameters) : null
        };

        var requestJson = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        _logger.LogDebug("Sending MCP request: {Request}", requestJson);

        await _stdin.WriteLineAsync(requestJson);
        await _stdin.FlushAsync();

        var responseLine = await _stdout.ReadLineAsync();
        if (responseLine == null)
        {
            throw new InvalidOperationException("No response from MCP server");
        }

        _logger.LogDebug("Received MCP response: {Response}", responseLine);

        return JsonSerializer.Deserialize<MCPResponse>(responseLine, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    public void Dispose()
    {
        try
        {
            _stdin?.Dispose();
            _stdout?.Dispose();
            
            if (_mcpServerProcess != null && !_mcpServerProcess.HasExited)
            {
                _mcpServerProcess.Kill();
                _mcpServerProcess.WaitForExit(5000);
            }
            
            _mcpServerProcess?.Dispose();
            _logger.LogInformation("MCP Client disconnected");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing MCP Client");
        }
    }
}