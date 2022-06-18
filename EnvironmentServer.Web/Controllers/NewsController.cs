using EnvironmentServer.DAL;
using EnvironmentServer.DAL.Models;
using EnvironmentServer.Web.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace EnvironmentServer.Web.Controllers
{
    public class NewsController : ControllerBase
    {
        public NewsController(Database db) : base(db) { }

        public IActionResult Index()
        {
            return View(DB.News.GetLatest(10));
        }

        [HttpPost]
        [Permission("news_write")]
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

        [Permission("news_write")]
        public IActionResult Delete(long id)
        {
            DB.News.Delete(id);
            return RedirectToAction("Index");
        }
    }
}
