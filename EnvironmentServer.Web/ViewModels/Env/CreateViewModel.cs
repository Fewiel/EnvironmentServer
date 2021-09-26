using EnvironmentServer.DAL.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EnvironmentServer.Web.ViewModels.Env
{
    public class CreateViewModel
    {
        [MinLength(4), MaxLength(32), DataType(DataType.Text)]
        [RegularExpression(@"^[a-z_]([a-z0-9_-]{0,31}|[a-z0-9_-]{0,30}\$)$", ErrorMessage = "Environment name not allowed")]
        public string EnvironmentName { get; set; }
        public string SWVersion { get; set; }
        public int Version { get; set; }
        public long TemplateID { get; set; }
        public string File { get; set; }
        public IEnumerable<SelectListItem> PhpVersions { get; set; }
        public IEnumerable<SelectListItem> Templates { get; set; }
    }
}
