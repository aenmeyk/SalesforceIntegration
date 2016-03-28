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
        public async Task<ActionResult> Index()
        {
            var salesforceService = new SalesforceService((ClaimsIdentity)User.Identity);
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
                var salesforceService = new SalesforceService((ClaimsIdentity)User.Identity);
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
            var salesforceService = new SalesforceService((ClaimsIdentity)User.Identity);
            await salesforceService.DeleteWebhookAsync(webhookModel);

            return RedirectToAction("Index");
        }
    }
}