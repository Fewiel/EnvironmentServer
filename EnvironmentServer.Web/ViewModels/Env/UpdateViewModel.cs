using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EnvironmentServer.Web.ViewModels.Env
{
    public class UpdateViewModel
    {
        public long ID { get; set; }
        public string EnvironmentName { get; set; }
        public string SWVersion { get; set; }
        public int Version { get; set; }
        public long TemplateID { get; set; }
        public string File { get; set; }
        public IEnumerable<SelectListItem> PhpVersions { get; set; }
        public IEnumerable<SelectListItem> Templates { get; set; }
    }
}
