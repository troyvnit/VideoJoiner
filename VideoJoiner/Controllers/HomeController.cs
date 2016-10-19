using System;
using System.IO;
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

        [HttpPost]
        public ActionResult Upload(string fileName)
        {
            try
            {
                foreach (string file in Request.Files)
                {
                    var fileContent = Request.Files[file];
                    if (fileContent != null && fileContent.ContentLength > 0)
                    {
                        var inputStream = fileContent.InputStream;
                        var path = Path.Combine(Server.MapPath("~/App_Data/"), fileName);
                        using (var fileStream = System.IO.File.Create(path))
                        {
                            inputStream.CopyTo(fileStream);
                        }
                    }
                }
            }
            catch (Exception)
            {
                return Json("Failed", JsonRequestBehavior.AllowGet);
            }
            return Json("Success", JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult Import(string fileName)
        {
            try
            {
                foreach (string file in Request.Files)
                {
                    var fileContent = Request.Files[file];
                    if (fileContent != null && fileContent.ContentLength > 0)
                    {
                        var inputStream = fileContent.InputStream;
                        using (var streamReader = new StreamReader(inputStream))
                        {
                            using (var db = new VideoJoinerContext())
                            {
                                string line;
                                while ((line = streamReader.ReadLine()) != null)
                                {
                                    if (!db.Videos.Any(v => v.SourceLink == line))
                                    {
                                        db.Videos.Add(new Video()
                                        {
                                            SourceLink = line
                                        });
                                    }
                                }

                                db.SaveChanges();
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                return Json("Failed", JsonRequestBehavior.AllowGet);
            }
            return Json("Success", JsonRequestBehavior.AllowGet);
        }

        public ActionResult ServerPerformance()
        {
            return View();
        }

    }
}