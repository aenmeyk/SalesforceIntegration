using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using RazorEngine;
using Salesforce.Common;
using Salesforce.Common.Models;
using Salesforce.Force;
using SalesforceIntegration.Models;

namespace SalesforceIntegration.Services
{
    public class SalesforceService
    {
        private string _clientId = ConfigurationManager.AppSettings["ConsumerKey"];
        private string _apiVersion = ConfigurationManager.AppSettings["ApiVersion"];
        private string _accessToken;
        private string _refreshToken;
        private string _instanceUrl;

        public SalesforceService(ClaimsIdentity user)
        {
            _accessToken = user.FindFirst(SalesforceClaims.AccessToken).Value;
            _refreshToken = user.FindFirst(SalesforceClaims.RefreshToken).Value;
            _instanceUrl = user.FindFirst(SalesforceClaims.InstanceUrl).Value;
        }

        public async Task<IEnumerable<SalesforceContact>> GetContactsAsync()
        {
            using (var client = new ForceClient(_instanceUrl, _accessToken, _apiVersion))
            {
                var contactsResult = await client.QueryAsync<SalesforceContact>("SELECT Id, FirstName, LastName, Email FROM Contact");
                return contactsResult.Records;
            }
        }

        public async Task UpdateContactAsync(SalesforceContact contact)
        {
            var updatedContact = new
            {
                FirstName = contact.FirstName,
                LastName = contact.LastName,
                Email = contact.Email,
                Phone = contact.Phone,
            };

            using (var client = new ForceClient(_instanceUrl, _accessToken, _apiVersion))
            {
                var success = await client.UpdateAsync("Contact", contact.Id, updatedContact);

                if (!success.Success)
                {
                    throw new HttpException((int)HttpStatusCode.InternalServerError, "Update Failed!");
                }
            }
        }

        public async Task<IEnumerable<WebhookModel>> GetWebhooksAsync()
        {
            var webhookModels = new Collection<WebhookModel>();
            QueryResult<ApexTrigger> apexTrigger;

            using (var client = new ForceClient(_instanceUrl, _accessToken, _apiVersion))
            {
                apexTrigger = await client.QueryAsync<ApexTrigger>("SELECT Id, Name, Body FROM ApexTrigger WHERE Name LIKE 'ActionRelayTrigger%'");
            }

            foreach (ApexTrigger record in apexTrigger.Records)
            {
                var webhookModel = new WebhookModel
                {
                    Id = record.Id,
                    Name = record.Name.ToString().Replace("ActionRelayTrigger", string.Empty),
                    SObject = record.Body.ToString().Split(' ')[3]
                };

                webhookModels.Add(webhookModel);
            }

            return webhookModels;
        }

        public async Task CreateSalesforceObjectsAsync(WebhookModel webhookModel)
        {
            await CreateWebhookClassAsync(webhookModel);
            await CreateTriggerAsync(webhookModel);
        }

        public async Task DeleteWebhookAsync(WebhookModel webhookModel)
        {
            using (var client = new ForceClient(_instanceUrl, _accessToken, _apiVersion))
            {
                var success = await client.DeleteAsync("ApexTrigger", webhookModel.Id);

                if (!success)
                {
                    throw new HttpException((int)HttpStatusCode.InternalServerError, "Delete Failed!");
                }
            }
        }

        public async Task<string> GetNewAccessTokenAsync(WebhookModel webhookModel)
        {
            var auth = new AuthenticationClient();
            await auth.TokenRefreshAsync(_clientId, _refreshToken);

            return auth.AccessToken;
        }

        private async Task CreateTriggerAsync(WebhookModel webhookModel)
        {
            var triggerBody = GetApexCode("SalesforceIntegration.ApexTemplates.TriggerTemplate.txt", webhookModel);

            var apexTrigger = new ApexTrigger
            {
                ApiVersion = _apiVersion.TrimStart('v'),
                Body = triggerBody,
                Name = "ActionRelayTrigger" + webhookModel.Name,
                TableEnumOrId = webhookModel.SObject
            };

            using (var client = new ForceClient(_instanceUrl, _accessToken, _apiVersion))
            {
                var success = await client.CreateAsync("ApexTrigger", apexTrigger);

                if (!success.Success)
                {
                    throw new HttpException((int)HttpStatusCode.InternalServerError, "Create Failed!");
                }
            }
        }

        private async Task CreateWebhookClassAsync(WebhookModel webhookModel)
        {
            using (var client = new ForceClient(_instanceUrl, _accessToken, _apiVersion))
            {
                var existingWebhookClass = await client.QueryAsync<ApexClass>("SELECT Id FROM ApexClass WHERE Name = 'ActionRelayWebhook'");

                if (!existingWebhookClass.Records.Any())
                {
                    var classBody = GetApexCode("SalesforceIntegration.ApexTemplates.WebhookTemplate.txt");

                    var apexClass = new ApexClass
                    {
                        ApiVersion = _apiVersion.TrimStart('v'),
                        Body = classBody,
                        Name = "ActionRelayWebhook"
                    };

                    var success = await client.CreateAsync("ApexClass", apexClass);

                    if (!success.Success)
                    {
                        throw new HttpException((int)HttpStatusCode.InternalServerError, "Create Failed!");
                    }
                }
            }
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
    }
}