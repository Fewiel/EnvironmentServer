using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnvironmentServer.DAL.Enums
{
    [Flags]
    public enum Timing
    {
        Custom = 0,
        Seconds = 1,
        Minutes = 2,
        Hours = 4,
        Days = 8,
        Weeks = 16,
        Months = 32,
        Years = 64
    }
}
