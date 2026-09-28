// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Staffs;

public class AnnotationResource
{
    public Guid ClientId { get; set; }

    public Guid Id { get; set; }

    public string Note { get; set; } = string.Empty;
}
