using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using Newtonsoft.Json;
using VideoJoiner.DataAccess;

namespace VideoJoiner.Controllers
{
    public class SettingController : ApiController
    {
        private VideoJoinerContext db = new VideoJoinerContext();

        // GET: api/Setting
        public IQueryable<Setting> GetSettings()
        {
            return db.Settings;
        }

        // GET: api/Setting/5
        [ResponseType(typeof(Setting))]
        public IHttpActionResult GetSetting(int id)
        {
            Setting setting = db.Settings.Find(id);
            if (setting == null)
            {
                return NotFound();
            }

            return Ok(setting);
        }

        // PUT: api/Setting/5
        [ResponseType(typeof(void))]
        public IHttpActionResult PutSetting(int id, Setting setting)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != setting.Id)
            {
                return BadRequest();
            }

            db.Entry(setting).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SettingExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST: api/Setting
        [ResponseType(typeof(Setting))]
        public IHttpActionResult PostSetting(List<Setting> settings)
        {
            //var settings = JsonConvert.DeserializeObject<List<Setting>>(rq);
            foreach (var setting in settings)
            {
                db.Entry(setting).State = EntityState.Modified;
            }
            db.SaveChanges();

            return Ok(settings);
        }

        // DELETE: api/Setting/5
        [ResponseType(typeof(Setting))]
        public IHttpActionResult DeleteSetting(int id)
        {
            Setting setting = db.Settings.Find(id);
            if (setting == null)
            {
                return NotFound();
            }

            db.Settings.Remove(setting);
            db.SaveChanges();

            return Ok(setting);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool SettingExists(int id)
        {
            return db.Settings.Count(e => e.Id == id) > 0;
        }
    }
}