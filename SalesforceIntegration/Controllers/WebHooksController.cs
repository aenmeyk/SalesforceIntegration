using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity.Owin;
using SalesforceIntegration.Models;
using SalesforceIntegration.Services;

namespace SalesforceIntegration.Controllers
{
    [Authorize]
    public class WebhooksController : Controller
    {
        private ApplicationSignInManager SignInManager
        {
            get { return HttpContext.GetOwinContext().Get<ApplicationSignInManager>(); }
        }

        public async Task<ActionResult> Index()
        {
            var salesforceService = new SalesforceService((ClaimsPrincipal)User, SignInManager);
            var webhookModels = await salesforceService.GetWebhooksAsync();

            return View(webhookModels);
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(WebhookModel webhookModel)
        {
            if (ModelState.IsValid)
            {
                var salesforceService = new SalesforceService((ClaimsPrincipal)User, SignInManager);
                await salesforceService.CreateSalesforceObjectsAsync(webhookModel);

                return RedirectToAction("Index");
            }

            return View(webhookModel);
        }

        public ActionResult Delete(WebhookModel webhookModel)
        {
            return View(webhookModel);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(WebhookModel webhookModel)
        {
            var salesforceService = new SalesforceService((ClaimsPrincipal)User, SignInManager);
            await salesforceService.DeleteWebhookAsync(webhookModel);

            return RedirectToAction("Index");
        }
    }
}