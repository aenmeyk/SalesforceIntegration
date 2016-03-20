using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using SalesforceIntegration.Models;

namespace SalesforceIntegration.Controllers
{
    [Authorize]
    public class WebhooksController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Webhooks
        public ActionResult Index()
        {
            return View(db.WebhookModels.ToList());
        }

        // GET: Webhooks/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            WebhookModel webhookModel = db.WebhookModels.Find(id);
            if (webhookModel == null)
            {
                return HttpNotFound();
            }
            return View(webhookModel);
        }

        // GET: Webhooks/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Webhooks/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(WebhookModel webhookModel)
        {
            if (ModelState.IsValid)
            {
                db.WebhookModels.Add(webhookModel);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(webhookModel);
        }

        // GET: Webhooks/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            WebhookModel webhookModel = db.WebhookModels.Find(id);
            if (webhookModel == null)
            {
                return HttpNotFound();
            }
            return View(webhookModel);
        }

        // POST: Webhooks/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(WebhookModel webhookModel)
        {
            if (ModelState.IsValid)
            {
                db.Entry(webhookModel).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(webhookModel);
        }

        // GET: Webhooks/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            WebhookModel webhookModel = db.WebhookModels.Find(id);
            if (webhookModel == null)
            {
                return HttpNotFound();
            }
            return View(webhookModel);
        }

        // POST: Webhooks/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            WebhookModel webhookModel = db.WebhookModels.Find(id);
            db.WebhookModels.Remove(webhookModel);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}