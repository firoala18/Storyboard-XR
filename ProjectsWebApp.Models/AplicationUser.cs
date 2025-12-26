using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectsWebApp.Models
{
    public class AplicationUser : IdentityUser
    {
        [Required]
        public String Name { get; set; }
    }
}
