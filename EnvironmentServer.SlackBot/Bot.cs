using EnvironmentServer.Interfaces;
using SlackAPI;

namespace EnvironmentServer.SlackBot;

public class Bot : IExternalMessaging
{
    public string Token { get; }

    private readonly SlackTaskClient Client;

    public Bot(string token)
    {
        Token = token;
        Client = new SlackTaskClient(token);
    }

    public async Task<bool> SendMessageAsync(string msg, string uid)
    {
        Console.WriteLine(msg);
        var response = await Client.PostMessageAsync(uid, msg);
        Console.Write(response.error);
        return response.ok;
    }
}