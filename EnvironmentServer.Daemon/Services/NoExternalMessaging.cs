using EnvironmentServer.Interfaces;
using System.Threading.Tasks;

namespace EnvironmentServer.Daemon.Services;

internal class NoExternalMessaging : IExternalMessaging
{
    public Task<bool> SendMessageAsync(string message, string channelID) => Task.FromResult(false);
}
