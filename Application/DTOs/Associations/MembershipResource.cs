// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
