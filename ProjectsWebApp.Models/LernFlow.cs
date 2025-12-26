using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProjectsWebApp.Models
{
    public class LernFlow
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(4000)]
        public string? Description { get; set; }

        // Optional cover image for the flow
        public string? ImagePath { get; set; }

        public string? OwnerId { get; set; }

        // Public access (same pattern as Storyboard)
        [Required, MaxLength(32)]
        public string PublicId { get; set; } = default!; // short slug

        [Required, MaxLength(64)]
        public string EditKeyHash { get; set; } = default!; // sha256 hex

        // Anonymous owner identity (hash of long-lived cookie token)
        [MaxLength(64)]
        public string? OwnerTokenHash { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;

        public List<LernStep> Steps { get; set; } = new();
    }
}
