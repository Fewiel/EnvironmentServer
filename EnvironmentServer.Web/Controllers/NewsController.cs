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

        [HttpPost]
        public IActionResult Add([FromForm]string content)
        {
            var news = new News
            {
                Content = content,
                UserID = GetSessionUser().ID
            };

            DB.News.Insert(news);

            return RedirectToAction("Index");
        }

        public IActionResult Delete(long id)
        {
            DB.News.Delete(id);
            return RedirectToAction("Index");
        }
    }
}
