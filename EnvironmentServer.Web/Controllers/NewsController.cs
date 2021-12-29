using EnvironmentServer.DAL;
using EnvironmentServer.DAL.Models;
using Microsoft.AspNetCore.Mvc;

namespace EnvironmentServer.Web.Controllers
{
    public class NewsController : ControllerBase
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

        public IActionResult Add(string content)
        {
            var news = new News
            {
                Content = content,
                UserID = GetSessionUser().ID
            };

            DB.News.Insert(news);

            return RedirectToAction("Index");
        }

    }
}
