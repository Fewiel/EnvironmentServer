using EnvironmentServer.DAL;
using EnvironmentServer.Web.Attributes;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace EnvironmentServer.Web.Controllers;

[Permission("logs")]
public class SystemLogsController : ControllerBase
{
    public SystemLogsController(Database db) : base(db)
    {
    }

    public async Task<IActionResult> IndexAsync()
    {
        var logs = await DB.Logs.Get();
        return View(logs);
    }
}
