using EnvironmentServer.DAL;
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
        public abstract Task ExecuteAsync(Database db, long variableID, long userID);
    }
}
