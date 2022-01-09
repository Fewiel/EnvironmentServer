namespace EnvironmentServer.Interfaces;

public interface IExternalMessaging
{
    Task<bool> SendMessageAsync(string message, string channelID);
}