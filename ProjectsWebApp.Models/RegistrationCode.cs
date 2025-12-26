using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectsWebApp.Models
{
    public class RegistrationCode
    {
        public int Id { get; set; }

        /// <summary>
        /// Der Einladungscode
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Ist der Code aktuell aktiv (gültig)?
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// (Optional) Beschreibung oder Notiz
        /// </summary>
        public string? Note { get; set; }
    }
}
