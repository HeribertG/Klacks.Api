// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.ComponentModel.DataAnnotations;
using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Settings
{
    public class Countries : BaseEntity
    {
        [StringLength(10)]
        public string Abbreviation { get; set; } = string.Empty;

        public MultiLanguage Name { get; set; } = null!;

        [StringLength(10)]
        public string Prefix { get; set; } = string.Empty;
    }
}
