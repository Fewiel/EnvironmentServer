using EnvironmentServer.DAL.Enums;
using EnvironmentServer.DAL.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EnvironmentServer.Web.ViewModels.EnvSetup
{
    public class EnvSetupViewModel
    {
        //DB Data
        [MinLength(4), MaxLength(32), DataType(DataType.Text)]
        [RegularExpression(@"^[a-z_]([a-z0-9_-]{0,31}|[a-z0-9_-]{0,30}\$)$", ErrorMessage = "Environment name not allowed")]
        public string Name { get; set; }
        public int MajorShopwareVersion { get; set; }
        public PhpVersion PhpVersion { get; set; }

        //View Data Single Use
        public string ShopwareVersion { get; set; }
        public string ShopwareVersionDownload { get; set; }
        public string WgetURL { get; set; }
        public string GitURL { get; set; }
        public string CustomSetupType { get; set; }

        //Display Data
        public IEnumerable<PhpVersion> PhpVersions { get; set; }
        public IEnumerable<ShopwareVersionInfo> ShopwareVersions { get; set; }
    }
}