using System.Configuration;
using System.Data.Entity;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Mvc;
using SalesforceIntegration.Models;
using SalesforceIntegration.Services;

namespace SalesforceIntegration.Controllers
{
    [Authorize]
    public class WebhooksController : Controller
    {
        private static readonly string SecurityToken = ConfigurationManager.AppSettings["SecurityToken"];
        private static readonly string ConsumerKey = ConfigurationManager.AppSettings["ConsumerKey"];
        private static readonly string ConsumerSecret = ConfigurationManager.AppSettings["ConsumerSecret"];
        private static readonly string Username = ConfigurationManager.AppSettings["Username"];
        private static readonly string Password = ConfigurationManager.AppSettings["Password"] + SecurityToken;
        private static readonly string IsSandboxUser = ConfigurationManager.AppSettings["IsSandboxUser"];
        private static readonly string ApiVersion = ConfigurationManager.AppSettings["ApiVersion"];

        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Webhooks
        public async Task<ActionResult> Index()
        {
            var salesforceService = new SalesforceService((ClaimsIdentity)User.Identity);
            var webhookModels = await salesforceService.GetWebhooks();

            return View(webhookModels);
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
        public async Task<ActionResult> Create(WebhookModel webhookModel)
        {
            if (ModelState.IsValid)
            {
                var salesforceService = new SalesforceService((ClaimsIdentity)User.Identity);
                await salesforceService.CreateSalesforceObjects(webhookModel);

                //db.WebhookModels.Add(webhookModel);
                //db.SaveChanges();
                return RedirectToAction("Index");
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
        public ActionResult Delete(string name)
        {
            //if (id == null)
            //{
            //    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            //}
            //WebhookModel webhookModel = db.WebhookModels.Find(id);
            //if (webhookModel == null)
            //{
            //    return HttpNotFound();
            //}
            //return View(webhookModel);

            return View();
        }

        // POST: Webhooks/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string name)
        {
            //WebhookModel webhookModel = db.WebhookModels.Find(id);
            //db.WebhookModels.Remove(webhookModel);
            //db.SaveChanges();
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