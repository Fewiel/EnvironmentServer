using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnvironmentServer.Daemon.Actions
{
    public abstract class ActionBase
    {
        public abstract string ActionIdentifier { get; }
        public abstract void Execute(long variableID, long userID);
    }
}
