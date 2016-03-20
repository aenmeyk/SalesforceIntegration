using System.ComponentModel.DataAnnotations;

namespace SalesforceIntegration.Models
{
    public class WebhookModel
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string SObject { get; set; }
        public string Url { get; set; }
    }
}