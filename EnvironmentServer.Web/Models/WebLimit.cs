using EnvironmentServer.DAL.Models;

namespace EnvironmentServer.Web.Models;

public class WebLimit
{
    public Limit Limit { get; set; }
    public int Value { get; set; }

    public Limit ToLimit() => new()
    {
        InternalName = Limit.InternalName,
        ID = Limit.ID,
        Name = Limit.Name
    };

    public static WebLimit FromLimit(Limit lim, int value = -1) => new()
    {
        Limit = lim,
        Value = value
    };
}