// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.ComponentModel.DataAnnotations;

namespace Klacks.Api.Application.DTOs.Associations
{
    public class MembershipResource
    {
        public Guid ClientId { get; set; }

        public Guid Id { get; set; }

        public int Type { get; set; }

        [Required]
        public DateTime ValidFrom { get; set; }

        public DateTime? ValidUntil { get; set; }
    }
}
