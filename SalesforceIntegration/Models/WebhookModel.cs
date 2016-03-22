using System.ComponentModel.DataAnnotations;

namespace SalesforceIntegration.Models
{
    public class WebhookModel
    {
        public string Name { get; set; }
        public string SObject { get; set; }
        public string Url { get; set; }
    }
}