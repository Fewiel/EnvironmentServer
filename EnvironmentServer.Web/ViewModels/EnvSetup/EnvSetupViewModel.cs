﻿using EnvironmentServer.DAL.Enums;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.Web.Controllers;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EnvironmentServer.Web.ViewModels.EnvSetup
{
    public class EnvSetupViewModel
    {
        //DB Data
        [MinLength(4), MaxLength(15), DataType(DataType.Text)]
        public string InternalName { get; set; }
        [MinLength(1), MaxLength(50), DataType(DataType.Text)]
        public string DisplayName { get; set; }
        public int MajorShopwareVersion { get; set; }
        public PhpVersion PhpVersion { get; set; }

        //View Data Single Use
        public string ShopwareVersion { get; set; }
        public string ShopwareVersionDownload { get; set; }
        public string Shopware6VersionDownload { get; set; }
        public string WgetURL { get; set; }
        public string GitURL { get; set; }
        public string CustomSetupType { get; set; }
        public string ExhibitionFile { get; set; }
        public long TemplateID { get; set; }
        public int WebRoutePath { get; set; }
        public int Language { get; set; }
        public int Currency { get; set; }

        //Display Data
        public IEnumerable<PhpVersion> PhpVersions { get; set; }
        public IEnumerable<SelectListItem> Currencies { get; set; }
        public IEnumerable<SelectListItem> Languages { get; set; }
        public IEnumerable<ShopwareVersionInfo> ShopwareVersions { get; set; }
        public IEnumerable<string> Shopware6Versions { get; set; }
        public IEnumerable<ExhibitionVersion> ExhibitionVersions { get; set;}
        public IEnumerable<Template> Templates { get; set; }
    }
}