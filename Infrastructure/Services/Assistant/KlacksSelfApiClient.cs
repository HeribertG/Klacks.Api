// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Calls the own REST API over HTTP on behalf of a skill, re-presenting the caller's bearer token so
/// that [Authorize], FluentValidation and the request log apply to an assistant-driven mutation exactly
/// as they do to one from the browser. Without a token the call is refused before it is sent — a skill
/// must never fall back to writing directly. Every request carries the skill name and a correlation id
/// so the log shows which skill caused which write.
/// </summary>
/// <param name="httpClient">Typed client, pre-configured with the loopback base address</param>
/// <param name="logger">Structured log of refused and failed self-calls</param>

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public sealed class KlacksSelfApiClient : IKlacksSelfApiClient
{
    public const string HttpClientName = "KlacksSelfApi";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly ILogger<KlacksSelfApiClient> _logger;

    public KlacksSelfApiClient(HttpClient httpClient, ILogger<KlacksSelfApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public Task<SelfApiResult<T>> PostAsync<T>(
        string route, object body, SkillExecutionContext context, string skillName, CancellationToken cancellationToken = default) =>
        SendAsync<T>(HttpMethod.Post, route, body, context, skillName, cancellationToken);

    public Task<SelfApiResult<T>> PutAsync<T>(
        string route, object body, SkillExecutionContext context, string skillName, CancellationToken cancellationToken = default) =>
        SendAsync<T>(HttpMethod.Put, route, body, context, skillName, cancellationToken);

    public Task<SelfApiResult<T>> DeleteAsync<T>(
        string route, SkillExecutionContext context, string skillName, CancellationToken cancellationToken = default) =>
        SendAsync<T>(HttpMethod.Delete, route, null, context, skillName, cancellationToken);

    private async Task<SelfApiResult<T>> SendAsync<T>(
        HttpMethod method,
        string route,
        object? body,
        SkillExecutionContext context,
        string skillName,
        CancellationToken cancellationToken)
    {
        if (context.AccessToken is null)
        {
            _logger.LogWarning(
                "Skill {SkillName} tried to call {Method} {Route} without a caller token; refused",
                skillName, method.Method, route);

            return SelfApiResult<T>.Failed(
                0,
                "This action could not be authorised because the request carried no access token. " +
                "Sign in again and retry.");
        }

        using var request = new HttpRequestMessage(method, route);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken.Value);
        request.Headers.TryAddWithoutValidation(SelfApiHeaders.SkillName, skillName);
        request.Headers.TryAddWithoutValidation(SelfApiHeaders.CorrelationId, ResolveCorrelationId(context));

        if (body is not null)
        {
            request.Content = JsonContent.Create(body, body.GetType(), options: SerializerOptions);
        }

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex, "Self-call {Method} {Route} for skill {SkillName} could not reach the API",
                method.Method, route, skillName);

            return SelfApiResult<T>.Failed(0, "The request could not be completed because the API was unreachable.");
        }

        using (response)
        {
            if (response.IsSuccessStatusCode)
            {
                return SelfApiResult<T>.Ok(
                    await ReadValueAsync<T>(response, cancellationToken),
                    (int)response.StatusCode);
            }

            var message = await DescribeFailureAsync(response, cancellationToken);
            _logger.LogWarning(
                "Self-call {Method} {Route} for skill {SkillName} failed with {StatusCode}",
                method.Method, route, skillName, (int)response.StatusCode);

            return SelfApiResult<T>.Failed((int)response.StatusCode, message);
        }
    }

    private static async Task<T?> ReadValueAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.StatusCode == HttpStatusCode.NoContent || response.Content.Headers.ContentLength == 0)
        {
            return default;
        }

        try
        {
            return await response.Content.ReadFromJsonAsync<T>(SerializerOptions, cancellationToken);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    private static async Task<string> DescribeFailureAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var problem = await ReadProblemAsync(response, cancellationToken);

        return response.StatusCode switch
        {
            HttpStatusCode.BadRequest =>
                problem ?? "The request was rejected as invalid.",
            HttpStatusCode.Unauthorized =>
                "The access token was not accepted, most likely because it expired. Sign in again and retry.",
            HttpStatusCode.Forbidden =>
                "Permission denied: your role is not allowed to perform this action.",
            HttpStatusCode.NotFound =>
                problem ?? "The referenced record does not exist.",
            HttpStatusCode.Conflict =>
                problem ?? "The record was changed by someone else in the meantime.",
            _ => problem ?? $"The request failed with status {(int)response.StatusCode}."
        };
    }

    private static async Task<string?> ReadProblemAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        string payload;
        try
        {
            payload = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (Exception)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var validationMessages = ReadValidationMessages(root);
            if (validationMessages.Count > 0)
            {
                return string.Join(" ", validationMessages);
            }

            if (root.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String)
            {
                return detail.GetString();
            }

            if (root.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
            {
                return title.GetString();
            }

            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static List<string> ReadValidationMessages(JsonElement root)
    {
        var messages = new List<string>();

        if (!root.TryGetProperty("errors", out var errors) || errors.ValueKind != JsonValueKind.Object)
        {
            return messages;
        }

        foreach (var field in errors.EnumerateObject())
        {
            if (field.Value.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var message in field.Value.EnumerateArray())
            {
                if (message.ValueKind == JsonValueKind.String)
                {
                    messages.Add(message.GetString()!);
                }
            }
        }

        return messages;
    }

    private static string ResolveCorrelationId(SkillExecutionContext context) =>
        string.IsNullOrWhiteSpace(context.SessionId) ? Guid.NewGuid().ToString() : context.SessionId!;
}
