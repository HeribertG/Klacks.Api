// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The shipped list of redirect targets the built-in OAuth server trusts without administrator action. Entries
/// are compared against the raw redirect string, never against a normalised URI. Before adding any entry, verify
/// that the page behind it is not a redirector: a page that forwards to an address taken from its own query
/// string or fragment (for example vscode.dev/redirect) would hand the authorization code to whoever builds the
/// link, so such a page may only be listed in the class whose own switch covers what it forwards to
/// (vscode.dev/redirect forwards to localhost and is therefore a local-program entry). Re-check every entry
/// against the vendor documentation when the list changes; the list ships with each release.
/// </summary>
using Klacks.Api.Domain.Models.Authentification;

namespace Klacks.Api.Domain.Constants;

public static class OAuthTrustedRedirects
{
    public const string ClaudeDisplayName = "Claude";

    public const string ChatGptDisplayName = "ChatGPT";

    public const string LocalProgramDisplayName = "Program on this computer";

    public const string ChatGptHost = "chatgpt.com";

    public const string ChatGptCallbackPrefix = "https://chatgpt.com/connector/oauth/";

    public const int ChatGptCallbackIdMaxLength = 128;

    public const string VsCodeWebHost = "vscode.dev";

    public const string VsCodeWebRedirectUri = "https://vscode.dev/redirect";

    public const string LoopbackHostLocalhost = "localhost";

    public const string LoopbackHostIpv4 = "127.0.0.1";

    public const string LoopbackHostIpv6 = "[::1]";

    public const string LoopbackScheme = "http://";

    public static readonly IReadOnlyList<string> LoopbackHosts = [LoopbackHostLocalhost, LoopbackHostIpv4, LoopbackHostIpv6];

    public const int MaxRedirectUriLength = 2048;

    public const int MaxPort = 65535;

    public const int MaxCustomTargets = 50;

    public static readonly IReadOnlyList<OAuthTrustedRedirectEntry> VendorEntries =
    [
        new(ClaudeDisplayName, "claude.ai", "https://claude.ai/api/mcp/auth_callback"),
        new(ClaudeDisplayName, "claude.com", "https://claude.com/api/mcp/auth_callback"),
        new(ChatGptDisplayName, ChatGptHost, "https://chatgpt.com/connector_platform_oauth_redirect")
    ];
}
