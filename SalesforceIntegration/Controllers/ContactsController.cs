using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Mvc;
using SalesforceIntegration.Models;
using SalesforceIntegration.Services;

namespace SalesforceIntegration.Controllers
{
    [Authorize]
    public class ContactsController : Controller
    {
        // GET: Contacts
        public async Task<ActionResult> Index()
        {
            var salesforceService = new SalesforceService((ClaimsIdentity)User.Identity);
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
                var salesforceService = new SalesforceService((ClaimsIdentity)User.Identity);
                await salesforceService.UpdateContactAsync(contact);

                return RedirectToAction("Index");
            }

            return View(contact);
        }
    }
}
