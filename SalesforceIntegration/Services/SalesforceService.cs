using System.Configuration;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RazorEngine;
using SalesforceIntegration.Models;

namespace SalesforceIntegration.Services
{
    public class SalesforceService
    {
        private string _apiVersion = ConfigurationManager.AppSettings["ApiVersion"];
        private string _salesforceAccessToken;

        public SalesforceService(ClaimsIdentity user)
        {
            var accessToken = user.FindFirst(SalesforceClaims.AccessToken);
            _salesforceAccessToken = accessToken.Value;
        }

        public async Task CreateSalesforceObjects(WebhookModel webhookModel)
        {
            var salesforceRestUrl = await GetSalesforceRestUrl();
            await CreateWebhookClass(webhookModel, salesforceRestUrl);
            await CreateTrigger(webhookModel, salesforceRestUrl);
        }

        private async Task<string> GetSalesforceRestUrl()
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _salesforceAccessToken);
                var userInfoResponse = await httpClient.GetAsync("https://login.salesforce.com/services/oauth2/userinfo");
                var content = await userInfoResponse.Content.ReadAsStringAsync();
                dynamic jObject = JObject.Parse(content);
                var restUrlTemplate = jObject.urls.rest.Value;
                var salesforceRestUrl = restUrlTemplate.Replace("{version}", _apiVersion);

                return salesforceRestUrl;
            }
        }

        private async Task CreateTrigger(WebhookModel webhookModel, string salesforceRestUrl)
        {
            var classBody = GetApexCode("SalesforceIntegration.ApexTemplates.TriggerTemplate.txt", webhookModel);

            var apexTrigger = new ApexTrigger
            {
                ApiVersion = _apiVersion,
                Body = classBody,
                Name = "ActionRelayTrigger" + webhookModel.Name,
                TableEnumOrId = webhookModel.SObject
            };

            var url = salesforceRestUrl + "tooling/sobjects/ApexTrigger";
            await PostApexObject(url, apexTrigger);
        }

        private async Task CreateWebhookClass(WebhookModel webhookModel, string salesforceRestUrl)
        {
            var classBody = GetApexCode("SalesforceIntegration.ApexTemplates.WebhookTemplate.txt");

            var apexClass = new ApexClass
            {
                ApiVersion = _apiVersion,
                Body = classBody,
                Name = "ActionRelayWebhook"
            };

            var url = salesforceRestUrl + "tooling/sobjects/ApexClass";
            await PostApexObject(url, apexClass);
        }

        private string GetApexCode(string templateName, object model = null)
        {
            var assembly = Assembly.GetExecutingAssembly();

            using (var stream = assembly.GetManifestResourceStream(templateName))
            using (var reader = new StreamReader(stream))
            {
                var template = reader.ReadToEnd();

                if (model != null)
                {
                    template = Razor.Parse(template, model);
                }

                return template;
            }
        }

        private async Task PostApexObject(string url, object apexObject)
        {
            var jsonApexObject = JsonConvert.SerializeObject(apexObject);
            var content = new StringContent(jsonApexObject);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _salesforceAccessToken);
                var response = await httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpException((int)response.StatusCode, "POST Request to Salesforce Failed!");
                }
            }
        }
    }
}