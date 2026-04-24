using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectsWebApp.Models
{
    public class Marker
    {
        public int Id { get; set; }

        // Relative coords 0..1
        [Range(0, 1)] public double X { get; set; }
        [Range(0, 1)] public double Y { get; set; }

        public int Number { get; set; }
        [MaxLength(9)] public string ColorHex { get; set; } = "#89ba17";

        // Rich-text HTML (TinyMCE) — unbounded so long/formatted content
        // doesn't blow past a varchar limit and produce a 500 on save.
        public string Description { get; set; } = string.Empty;
        public string Ziel { get; set; } = string.Empty;
        public string Datenablage { get; set; } = string.Empty;
        public string Quellen { get; set; } = string.Empty;
        public string PromptIdee { get; set; } = string.Empty;
        public string Reflexion { get; set; } = string.Empty;
        [MaxLength(200)] public string Model { get; set; } = string.Empty;

        // NEW: Individual taxonomy level per marker (limited to storyboard's maximum)
        public TaxonomieStufe? Taxonomie { get; set; }

        // NEW: scope to a Scene (not storyboard)
        public int SceneId { get; set; }
        public Scene? Scene { get; set; }

        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}
