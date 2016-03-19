using System.Configuration;
using System.Web.Mvc;

namespace SalesforceIntegration.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            var oauthUri = "https://login.salesforce.com/services/oauth2/authorize?response_type=code&client_id=%s&redirect_uri=%s&state=%s";
            var consumerKey = ConfigurationManager.AppSettings["ConsumerKey"];
            var consumerSecret = ConfigurationManager.AppSettings["ConsumerSecret"];
            var redirectUri = "https://salesforce-webhook-creator.herokuapp.com/_oauth_callback";
            var state = "prod";

            ViewBag.LoginUrl = "https://login.salesforce.com/services/oauth2/authorize?response_type=code&client_id=%s&redirect_uri=%s&state=%s";

            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}