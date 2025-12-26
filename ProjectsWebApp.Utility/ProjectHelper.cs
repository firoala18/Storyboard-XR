using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectsWebApp.Utility
{
    public static class ProjectHelper
    {
        public static string GetFormattedStatus(string status)
        {
            return status switch
            {
                "Anstehend" => "Anstehend",
                "InBearbeitung" => "In Bearbeitung",
                "InEntwicklung" => "In Entwicklung",
                "Ausgesetzt" => "Ausgesetzt",
                "Abgeschlossen" => "Abgeschlossen",
                "Archiviert" => "Archiviert",
                _ => "Unbekannter Status"
            };
        }
    }

}
