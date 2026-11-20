// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.ComponentModel.DataAnnotations;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Assistant;

/// <summary>
/// One stable pseudonym of the installation: the number a person or an ambiguous name carries on the line to a cloud
/// language model. The key is an entity id, never a plain value; detected values (phone, e-mail) are numbered per request and never stored. The table is the additional
/// information in the sense of Art. 4 No. 5 GDPR and never leaves the installation.
/// </summary>
public class LLMPseudonym
{
    [Key]
    public Guid Id { get; set; }

    public PseudonymKind Kind { get; set; }

    [Required]
    [MaxLength(LLMPseudonymFieldLimits.KeyMaxLength)]
    public string Key { get; set; } = string.Empty;

    public int Ordinal { get; set; }

    public DateTime CreateTime { get; set; }
}
