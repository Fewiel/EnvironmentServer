using EnvironmentServer.DAL;
using EnvironmentServer.Web.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace EnvironmentServer.Web.Controllers;

[Permission("logs")]
public class SystemLogsController : ControllerBase
{
    public SystemLogsController(Database db) : base(db)
    {
    }

    public IActionResult Index()
    {
        return View(DB.Logs.Get());
    }
}
