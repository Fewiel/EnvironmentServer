using EnvironmentServer.DAL.Models;

namespace EnvironmentServer.Web.Models;

public class WebLimit
{
    public Limit Limit { get; set; }
    public int Value { get; set; }
}