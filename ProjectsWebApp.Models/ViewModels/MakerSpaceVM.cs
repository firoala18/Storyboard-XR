using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ProjectsWebApp.Models.ViewModels
{
    public class MakerSpaceVM
    {
        public MakerSpaceProject MakerSpaceProject { get; set; }
        //the variable name must match what in the product creat function
       

        [BindNever]
        [ValidateNever]
        public List<string> ExistingTags { get; set; }

       



    }
}
