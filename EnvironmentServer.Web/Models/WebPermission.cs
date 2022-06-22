using EnvironmentServer.DAL.Models;

namespace EnvironmentServer.Web.Models;

public class WebPermission
{
    public Permission Permission { get; set; }
    public bool Enabled { get; set; }

    public Permission ToPermission() => new()
    {
        ID = Permission.ID,
        Name = Permission.Name,
        InternalName = Permission.InternalName
    };

    public static WebPermission FromPermission(Permission perm, bool enabled = false) => new()
    {
        Permission = perm,
        Enabled = enabled
    };
}