using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RazorEngine;
using Salesforce.Common;
using Salesforce.Force;
using SalesforceIntegration.Models;

namespace SalesforceIntegration.Services
{
    public class SalesforceService
    {
        private string _apiVersion = ConfigurationManager.AppSettings["ApiVersion"];
        private string _accessToken;
        private string _instanceUrl;

        public SalesforceService(ClaimsIdentity user)
        {
            _accessToken = user.FindFirst(SalesforceClaims.AccessToken).Value;
            _instanceUrl = user.FindFirst(SalesforceClaims.InstanceUrl).Value;
        }

        public async Task<IEnumerable<SalesforceContact>> GetContacts()
        {
            var client = new ForceClient(_instanceUrl, _accessToken, "v" + _apiVersion);
            var contactsResult = await client.QueryAsync<SalesforceContact>("SELECT Id, FirstName, LastName, Email FROM Contact");

            return contactsResult.Records;
        }

        public async Task UpdateContact(SalesforceContact contact)
        {
            var updatedContact = new
            {
                FirstName = contact.FirstName,
                LastName = contact.LastName,
                Email = contact.Email,
                Phone = contact.Phone,
            };

            var client = new ForceClient(_instanceUrl, _accessToken, "v" + _apiVersion);
            var success = await client.UpdateAsync("Contact", contact.Id, updatedContact);

            if (!success.Success)
            {
                throw new HttpException((int)HttpStatusCode.InternalServerError, "Update Failed!");
            }
        }

        public async Task<IEnumerable<WebhookModel>> GetWebhooks()
        {
            var salesforceRestUrl = await GetSalesforceRestUrl();
            var builder = new UriBuilder(salesforceRestUrl + "tooling/query");
            builder.Port = -1;
            var query = HttpUtility.ParseQueryString(builder.Query);
            query["q"] = "SELECT Name, Body FROM ApexTrigger WHERE Name LIKE 'ActionRelayTrigger%'";
            builder.Query = query.ToString();
            var url = builder.ToString();

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                var response = await httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpException((int)response.StatusCode, "GET Failed!");
                }

                var content = await response.Content.ReadAsStringAsync();
                dynamic jObject = JObject.Parse(content);
                var records = jObject.records;
                var webhookModels = new Collection<WebhookModel>();

                foreach (dynamic record in records)
                {
                    var webhookModel = new WebhookModel
                    {
                        Name = record.Name,
                        SObject = record.Body.ToString().Split(' ')[3],
                        Location = record.attributes.url
                    };

                    webhookModels.Add(webhookModel);
                }

                return webhookModels;
            }
        }

        public async Task CreateSalesforceObjects(WebhookModel webhookModel)
        {
            var salesforceRestUrl = await GetSalesforceRestUrl();
            await CreateWebhookClass(webhookModel, salesforceRestUrl);
            await CreateTrigger(webhookModel, salesforceRestUrl);
        }

        public async Task DeleteWebhook(WebhookModel webhookModel)
        {
            var salesforceRestUrl = await GetSalesforceRestUrl();
            var salesforceRestUri = new Uri(salesforceRestUrl);
            var authority = salesforceRestUri.GetLeftPart(UriPartial.Authority);
            var url = authority + webhookModel.Location;

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                var response = await httpClient.DeleteAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpException((int)response.StatusCode, "DELETE Failed!");
                }
            }
        }

        private async Task<string> GetSalesforceRestUrl()
        {
            dynamic userInfo = await GetSalesforceUserInfo();
            var restUrlTemplate = userInfo.urls.rest.Value;
            var salesforceRestUrl = restUrlTemplate.Replace("{version}", _apiVersion);

            return salesforceRestUrl;
        }

        private async Task<string> GetSalesforceSObjectsUrl()
        {
            dynamic userInfo = await GetSalesforceUserInfo();
            var restUrlTemplate = userInfo.urls.sobjects.Value;
            var salesforceSObjectsUrl = restUrlTemplate.Replace("{version}", _apiVersion);

            return salesforceSObjectsUrl;
        }

        private async Task<JObject> GetSalesforceUserInfo()
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                var userInfoResponse = await httpClient.GetAsync("https://login.salesforce.com/services/oauth2/userinfo");
                var content = await userInfoResponse.Content.ReadAsStringAsync();
                var userInfo = JObject.Parse(content);

                return userInfo;
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
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                var response = await httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpException((int)response.StatusCode, "POST Failed!");
                }
            }
        }
    }
}