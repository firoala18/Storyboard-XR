using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectsWebApp.Models
{
    public enum TaxonomieStufe
    {
        Erinnern = 0,
        Verstehen = 1,
        Anwenden = 2,
        Analysieren = 3,
        Bewerten = 4,
        Erschaffen = 5
    }

    public enum LicenseType
    {
        Attribution_CC_BY,
        Attribution_ShareAlike_CC_BY_SA,
        Public_Domain_Dedication_CC0,
        Copyright,
        MIT
    }

    public class Storyboard
    {
        public int Id { get; set; }

       
        public string Title { get; set; } = string.Empty;
        // Legacy cover storage (kept for backwards compatibility)
        public string? ImagePath { get; set; }
        // New dedicated cover image field
        public string? CoverImagePath { get; set; }

        public string Zielgruppe { get; set; } = string.Empty;
        public string Beschreibung { get; set; } = string.Empty;
        public string Lernziel { get; set; } = string.Empty;
        [MaxLength(2000)] public string Farbpalette { get; set; } = string.Empty;

        // Optional: maximum selected Taxonomie-Stufe; implies inclusion of all previous stages
        public TaxonomieStufe? Taxonomie { get; set; }

        // --- Licensing & Authors ---
        public LicenseType? License { get; set; }

        // Multiple author names stored as a comma-separated string
        [MaxLength(1024)]
        public string? Authors { get; set; }

        // Free-text notes about the license, attribution details, URLs, etc.
        [MaxLength(2000)]
        public string? LicenseExtras { get; set; }

        public string? OwnerId { get; set; }

        // Link-based access (already in your model)
        [Required, MaxLength(32)]
        public string PublicId { get; set; } = default!;

        [Required, MaxLength(64)]
        public string EditKeyHash { get; set; } = default!;

        public bool Readonly { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;

        [MaxLength(64)] public string? AccessTokenView { get; set; }
        [MaxLength(64)] public string? AccessTokenEdit { get; set; }

        // ✅ NEW: anonymous “owner” identity (hash of long-lived cookie token)
        [MaxLength(64)]
        public string? OwnerTokenHash { get; set; }

        public List<Scene> Scenes { get; set; } = new();
    }
}
