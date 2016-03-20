using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SalesforceIntegration.Models
{
    public class WebhookModel
    {
        public WebhookModel()
        {
            this.Events = new List<string>();
        }

        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string SObject { get; set; }
        public IList<string> Events { get; set; }
        public string Url { get; set; }
    }
}