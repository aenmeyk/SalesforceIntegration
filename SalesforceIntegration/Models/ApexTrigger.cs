namespace SalesforceIntegration.Models
{
    public class ApexTrigger
    {
        public string ApiVersion { get; set; }
        public string Body { get; set; }
        public string Name { get; set; }
        public string TableEnumOrId { get; set; }
    }
}