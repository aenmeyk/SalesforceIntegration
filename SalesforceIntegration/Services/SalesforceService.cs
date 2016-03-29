using System;
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
    /// <summary>
    /// Contains some POC methods to demonstrate various interactions with Salesforce.
    /// </summary>
    public class SalesforceService
    {
        private string _clientId = ConfigurationManager.AppSettings["ConsumerKey"];
        private string _apiVersion = ConfigurationManager.AppSettings["ApiVersion"];
        private ClaimsPrincipal _user;
        private ApplicationSignInManager _signInManager;

        private string AccessToken
        {
            get { return _user.FindFirst(SalesforceClaims.AccessToken).Value; }
        }

        private string RefreshToken
        {
            get { return _user.FindFirst(SalesforceClaims.RefreshToken).Value; }
        }

        private string InstanceUrl
        {
            get { return _user.FindFirst(SalesforceClaims.InstanceUrl).Value; }
        }

        public SalesforceService(ClaimsPrincipal user, ApplicationSignInManager signInManager)
        {
            _user = user;
            _signInManager = signInManager;
        }

        /// <summary>
        /// Demonstrates using the refresh token to get a new access token if the current access token has expired.
        /// </summary>
        private async Task GetClientWithRefresh(Func<ForceClient, Task> useClient)
        {
            try
            {
                await ExecuteClient(useClient);
            }
            catch (ForceException ex)
            {
                if (ex.Message == "Session expired or invalid")
                {
                    await RefreshAccessTokenAsync();
                    await ExecuteClient(useClient);
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task ExecuteClient(Func<ForceClient, Task> useClient)
        {
            using (var client = new ForceClient(InstanceUrl, AccessToken, _apiVersion))
            {
                await useClient(client);
            }
        }

        /// <summary>
        /// Demonstrates retrieving data from a client's Salesforce instance using an access token.
        /// </summary>
        public async Task<IEnumerable<SalesforceContact>> GetContactsAsync()
        {
            IEnumerable<SalesforceContact> contacts = null;

            await GetClientWithRefresh(async client =>
            {
                var contactsResult = await client.QueryAsync<SalesforceContact>("SELECT Id, FirstName, LastName, Email, Phone FROM Contact");
                contacts = contactsResult.Records;
            });

            return contacts;
        }

        /// <summary>
        /// Demonstrates updating a contact Contact in a client's Salesforce instance
        /// </summary>
        public async Task UpdateContactAsync(SalesforceContact contact)
        {
            var updatedContact = new
            {
                FirstName = contact.FirstName,
                LastName = contact.LastName,
                Email = contact.Email,
                Phone = contact.Phone,
            };

            await GetClientWithRefresh(async client =>
            {
                var success = await client.UpdateAsync("Contact", contact.Id, updatedContact);

                if (!success.Success)
                {
                    throw new HttpException((int)HttpStatusCode.InternalServerError, "Update Failed!");
                }
            });
        }

        /// <summary>
        /// Demonstrates retrieving Apex trigger code from a client's Salesforce instance.
        /// </summary>
        public async Task<IEnumerable<WebhookModel>> GetWebhooksAsync()
        {
            var webhookModels = new Collection<WebhookModel>();
            QueryResult<ApexTrigger> apexTrigger = null;

            await GetClientWithRefresh(async client =>
            {
                apexTrigger = await client.QueryAsync<ApexTrigger>("SELECT Id, Name, Body FROM ApexTrigger WHERE Name LIKE 'ActionRelayTrigger%'");
            });

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
            await GetClientWithRefresh(async client =>
            {
                await CreateWebhookClassAsync(webhookModel, client);
                await CreateTriggerAsync(webhookModel, client);
            });
        }

        /// <summary>
        /// Demonstrates deleting an Apex trigger from a client's Salesforce instance.
        /// </summary>
        public async Task DeleteWebhookAsync(WebhookModel webhookModel)
        {
            await GetClientWithRefresh(async client =>
            {
                var success = await client.DeleteAsync("ApexTrigger", webhookModel.Id);

                if (!success)
                {
                    throw new HttpException((int)HttpStatusCode.InternalServerError, "Delete Failed!");
                }
            });
        }

        private async Task RefreshAccessTokenAsync()
        {
            // Refresh the token
            var auth = new AuthenticationClient();
            await auth.TokenRefreshAsync(_clientId, RefreshToken);

            // Get the current user ID
            var identity = (ClaimsIdentity)_user.Identity;
            var userId = identity.FindFirst(ClaimTypes.NameIdentifier).Value;

            // Remove the current AccessToken claim
            var currentClaims = await _signInManager.UserManager.GetClaimsAsync(userId);
            var currentClaim = currentClaims.Single(x => x.Type == SalesforceClaims.AccessToken);
            await _signInManager.UserManager.RemoveClaimAsync(userId, currentClaim);
            currentClaim = identity.FindFirst(SalesforceClaims.AccessToken);
            identity.RemoveClaim(currentClaim);

            // Add the new AccessToken claim
            var newClaim = new Claim(SalesforceClaims.AccessToken, auth.AccessToken);
            await _signInManager.UserManager.AddClaimAsync(userId, newClaim);
            identity.AddClaim(newClaim);
            var user = await _signInManager.UserManager.FindByIdAsync(userId);
            await _signInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
        }

        /// <summary>
        /// Demonstrates creating an Apex trigger in a client's Salesforce instance.
        /// </summary>
        private async Task CreateTriggerAsync(WebhookModel webhookModel, ForceClient client)
        {
            var triggerBody = GetApexCode("SalesforceIntegration.ApexTemplates.TriggerTemplate.txt", webhookModel);

            var apexTrigger = new ApexTrigger
            {
                ApiVersion = _apiVersion.TrimStart('v'),
                Body = triggerBody,
                Name = "ActionRelayTrigger" + webhookModel.Name,
                TableEnumOrId = webhookModel.SObject
            };

            var success = await client.CreateAsync("ApexTrigger", apexTrigger);

            if (!success.Success)
            {
                throw new HttpException((int)HttpStatusCode.InternalServerError, "Create Failed!");
            }
        }

        /// <summary>
        /// Demonstrates creating an Apex class in a client's Salesforce instance.
        /// </summary>
        private async Task CreateWebhookClassAsync(WebhookModel webhookModel, ForceClient client)
        {
            // First check if a class with this name already exists
            var existingWebhookClass = await client.QueryAsync<ApexClass>("SELECT Id FROM ApexClass WHERE Name = 'ActionRelayWebhook'");

            // If the class does not exist
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

        /// <summary>
        /// Demonstrates using RazorEngine to populate a Razor template values from a model.
        /// </summary>
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