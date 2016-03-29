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
    public class ContactsController : Controller
    {
        private ApplicationSignInManager SignInManager
        {
            get { return HttpContext.GetOwinContext().Get<ApplicationSignInManager>(); }
        }

        // GET: Contacts
        public async Task<ActionResult> Index()
        {
            var salesforceService = new SalesforceService((ClaimsPrincipal)User, SignInManager);
            var contacts = await salesforceService.GetContactsAsync();

            return View(contacts);
        }

        [HttpGet]
        public ActionResult Get(SalesforceContact contact)
        {
            return View("Edit", contact);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(SalesforceContact contact)
        {
            if (ModelState.IsValid)
            {
                var salesforceService = new SalesforceService((ClaimsPrincipal)User, SignInManager);
                await salesforceService.UpdateContactAsync(contact);

                return RedirectToAction("Index");
            }

            return View(contact);
        }
    }
}
