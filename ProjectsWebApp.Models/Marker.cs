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
        [MaxLength(9)] public string ColorHex { get; set; } = "#78a7ff";

        [MaxLength(2000)] public string Description { get; set; } = string.Empty;
        [MaxLength(2000)] public string Ziel { get; set; } = string.Empty;
        [MaxLength(2000)] public string Datenablage { get; set; } = string.Empty;
        [MaxLength(2000)] public string Quellen { get; set; } = string.Empty;
        [MaxLength(2000)] public string PromptIdee { get; set; } = string.Empty;
        [MaxLength(2000)] public string Reflexion { get; set; } = string.Empty;
        [MaxLength(200)] public string Model { get; set; } = string.Empty;

        // NEW: Individual taxonomy level per marker (limited to storyboard's maximum)
        public TaxonomieStufe? Taxonomie { get; set; }

        // NEW: scope to a Scene (not storyboard)
        public int SceneId { get; set; }
        public Scene? Scene { get; set; }
    }
}
