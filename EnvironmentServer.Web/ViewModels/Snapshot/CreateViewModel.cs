using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EnvironmentServer.Web.ViewModels.Snapshot
{
    public class CreateViewModel
    {
        public long EnvironmentId { get; set; }
        public string EnvironmentName { get; set; }

        [MinLength(4), MaxLength(32), DataType(DataType.Text)]
        [RegularExpression(@"^[a-z_]([a-z0-9_-]{0,31}|[a-z0-9_-]{0,30}\$)$", ErrorMessage = "Snapshot name not allowed")]
        public string Name { get; set; }
        public string Hash { get; set; }
        public bool Template { get; set; }
        public DateTimeOffset Created { get; set; }
    }
}
