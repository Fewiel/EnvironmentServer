using EnvironmentServer.DAL;
using Microsoft.AspNetCore.Mvc;

namespace EnvironmentServer.Web.Controllers
{
    public class NewsController : Controller
    {
        private Database DB;

        public NewsController(Database db)
        {
            DB = db;
        }

        public IActionResult Index()
        {
            return View(DB.News.GetLatest());
        }
    }
}
