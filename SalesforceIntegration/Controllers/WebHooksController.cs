using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using SalesforceIntegration.Models;
using Salesforce.Force;
using System.Configuration;
using System.Threading.Tasks;
using Salesforce.Common;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System.Security.Claims;
using System.Web;
using Microsoft.Owin.Security;

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
        public async Task<ActionResult> Create(WebhookModel webhookModel)
        {
            if (ModelState.IsValid)
            {
                //var auth = new AuthenticationClient();

                //// Authenticate with Salesforce
                //var url = IsSandboxUser.Equals("true", StringComparison.CurrentCultureIgnoreCase)
                //    ? "https://test.salesforce.com/services/oauth2/token"
                //    : "https://login.salesforce.com/services/oauth2/token";

                //await auth.UsernamePasswordAsync(ConsumerKey, ConsumerSecret, Username, Password, url);
                //var client = new ForceClient(auth.InstanceUrl, auth.AccessToken, auth.ApiVersion);
                //await client.ExecuteRestApiAsync("tooling/sobjects/ApexClass", )


                var accessToken = ((ClaimsIdentity)User.Identity).FindFirst(SalesforceClaims.AccessToken);
                var userInfoClient = new HttpClient();
                userInfoClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Value);
                var response = await userInfoClient.GetAsync("https://login.salesforce.com/services/oauth2/userinfo");
                var responseContent = await response.Content.ReadAsStringAsync();
                dynamic jObject = JObject.Parse(responseContent);
                var restUrlTemplate = jObject.urls.rest.Value;
                var restUrl = restUrlTemplate.Replace("{version}", ApiVersion);
                var apexClassUrl = restUrl + "tooling/sobjects/ApexClass";

                //var classBody = "public class Messages {\n"
                //   + "public string SayHello() {\n"
                //   + " return 'Hello';\n" + "}\n"
                //   + "}";

                //var content = new StringContent(classBody);
                //content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                //var httpClient = new HttpClient();
                //httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

                //await httpClient.PostAsync(auth.InstanceUrl, content);










                //db.WebhookModels.Add(webhookModel);
                //db.SaveChanges();
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