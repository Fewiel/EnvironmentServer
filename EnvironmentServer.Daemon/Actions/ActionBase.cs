using EnvironmentServer.DAL;
using Microsoft.Extensions.DependencyInjection;
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
        public abstract Task ExecuteAsync(ServiceProvider db, long variableID, long userID);
    }
}
