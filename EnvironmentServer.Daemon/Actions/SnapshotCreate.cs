using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnvironmentServer.Daemon.Actions
{
    public class SnapshotCreate : ActionBase
    {
        public override string ActionIdentifier => "snapshot_create";

        public override void Execute(long variableID, long userID)
        {
            Console.WriteLine("Ich bin eine Action (snapcreate)");
        }
    }
}
