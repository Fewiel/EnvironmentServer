using EnvironmentServer.DAL.Models;
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
        public string DisplayName { get; set; }
        public int Version { get; set; }
        public IEnumerable<SelectListItem> PhpVersions { get; set; }
    }
}
