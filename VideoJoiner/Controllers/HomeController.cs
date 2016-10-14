using System.Linq;
using System.Web.Mvc;
using VideoJoiner.DataAccess;

namespace VideoJoiner.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Setting()
        {
            return View();
        }

        public ActionResult Content()
        {
            return View();
        }

        public ActionResult VideoJoiner()
        {
            return View();
        }

        public ActionResult ServerPerformance()
        {
            return View();
        }

    }
}