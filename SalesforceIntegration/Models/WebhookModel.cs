using System.ComponentModel.DataAnnotations;

namespace SalesforceIntegration.Models
{
    public class WebhookModel
    {
        public string Id { get; set; }

        /// <summary>
        /// This will be used to generate the trigger name. 
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// The Salesforce object that the trigger with monitor. E.g. "Contact" or "Account"
        /// </summary>
        [Required]
        public string SObject { get; set; }

        /// <summary>
        /// The URL that the trigger with POST to when the SObject is created or updated
        /// </summary>
        [Required]
        public string Url { get; set; }
    }
}