// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.ComponentModel.DataAnnotations;

namespace Klacks.Api.Domain.Models.Settings;

public class PluginDoc
{
    [Key]
    public Guid Id { get; set; }
    public string PluginCode { get; set; } = string.Empty;
    public string ManualName { get; set; } = string.Empty;
    public string HtmlContent { get; set; } = string.Empty;
}
