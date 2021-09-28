using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnvironmentServer.DAL.Models
{
    public class EnvironmentSnapshot
    {
        public long Id { get; set; }
        public long EnvironmentId { get; set; }
        public string Name { get; set; }
        public string Hash { get; set; }
        public bool Template { get; set; }
        public DateTimeOffset Created { get; set; }
    }
}
